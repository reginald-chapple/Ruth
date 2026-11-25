using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class AutoPopulateServingScheduleCommandHandler(
    GroupsDbContext dbContext,
    ScheduleVolunteerService scheduleService
) : ICommandHandler<AutoPopulateServingScheduleCommand, AutoPopulateScheduleResponse>
{
    public async Task<Result<AutoPopulateScheduleResponse>> HandleAsync(
        AutoPopulateServingScheduleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(command.GroupId);
        var group = await dbContext
            .Groups.AsNoTracking()
            .Include(static g => g.Locations)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var context = await LoadPopulationContextAsync(group, cancellationToken);
        var created = new List<ScheduledVolunteerResponse>();

        foreach (var config in context.Configs)
        {
            var location = context.LocationsById[config.GroupLocationId];
            var targetCount = config.DesiredCapacity ?? config.MinimumCapacity ?? 0;
            if (targetCount > 0)
            {
                await PopulateSlotAsync(
                    group.Id,
                    location.LocationId,
                    config.ScheduleId,
                    targetCount,
                    command,
                    context,
                    created,
                    cancellationToken
                );
            }
        }

        return Result.Success(
            new AutoPopulateScheduleResponse(
                command.GroupId,
                command.OccurrenceDate,
                context.Configs.Count,
                created.Count,
                created
            )
        );
    }

    private async Task<PopulationContext> LoadPopulationContextAsync(
        Group group,
        CancellationToken cancellationToken
    )
    {
        var locationsById = group.Locations.ToDictionary(static l => l.Id);
        GroupLocationId[] glIds = [.. locationsById.Keys];
        var configs = await dbContext
            .LocationScheduleConfigs.AsNoTracking()
            .Where(c => glIds.Contains(c.GroupLocationId))
            .ToListAsync(cancellationToken);
        var members = await dbContext
            .GroupMembers.AsNoTracking()
            .Where(m =>
                m.GroupId == group.Id && !m.IsArchived && m.Status == GroupMemberStatus.Active
            )
            .ToListAsync(cancellationToken);
        var templates = await dbContext
            .ScheduleTemplates.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, cancellationToken);
        var assignments = await dbContext
            .GroupMemberAssignments.AsNoTracking()
            .Where(a => a.GroupId == group.Id)
            .ToListAsync(cancellationToken);
        return new PopulationContext(locationsById, configs, members, templates, assignments);
    }

    private async Task PopulateSlotAsync(
        GroupId groupId,
        Guid locationId,
        Guid scheduleId,
        int targetCount,
        AutoPopulateServingScheduleCommand command,
        PopulationContext context,
        List<ScheduledVolunteerResponse> created,
        CancellationToken cancellationToken
    )
    {
        var existingCount = await CountExistingScheduledAsync(
            groupId,
            locationId,
            scheduleId,
            command.OccurrenceDate,
            cancellationToken
        );
        var needed = targetCount - existingCount;
        if (needed <= 0)
        {
            return;
        }

        foreach (
            var member in OrderCandidates(context, locationId, scheduleId, command.OccurrenceDate)
        )
        {
            if (needed <= 0)
            {
                break;
            }

            var scheduled = await scheduleService.ScheduleAsync(
                new ScheduleVolunteerCommand(
                    command.GroupId,
                    member.PersonId,
                    command.OccurrenceDate,
                    locationId,
                    scheduleId,
                    command.ScheduledByPersonId,
                    ForceOverrideConflicts: false
                ),
                cancellationToken
            );
            if (scheduled.IsSuccess)
            {
                created.Add(scheduled.Value);
                needed--;
            }
        }
    }

    private async Task<int> CountExistingScheduledAsync(
        GroupId groupId,
        Guid locationId,
        Guid scheduleId,
        DateOnly occurrenceDate,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .AttendanceOccurrences.AsNoTracking()
            .Where(o =>
                o.GroupId == groupId
                && o.LocationId == locationId
                && o.ScheduleId == scheduleId
                && o.OccurrenceDate == occurrenceDate
            )
            .SelectMany(static o => o.Attendances)
            .CountAsync(
                static a => a.RequestedToAttend == true && a.Rsvp != RsvpStatus.No,
                cancellationToken
            );
    }

    private static IEnumerable<GroupMember> OrderCandidates(
        PopulationContext context,
        Guid locationId,
        Guid scheduleId,
        DateOnly occurrenceDate
    )
    {
        return context
            .Members.Where(m => MatchesTemplate(m, context.Templates, occurrenceDate))
            .OrderByDescending(m =>
                HasMatchingAssignment(m.Id, context.Assignments, locationId, scheduleId)
            );
    }

    private static bool MatchesTemplate(
        GroupMember member,
        Dictionary<GroupMemberScheduleTemplateId, GroupMemberScheduleTemplate> templates,
        DateOnly occurrenceDate
    )
    {
        return !member.ScheduleTemplateId.HasValue
            || !templates.TryGetValue(member.ScheduleTemplateId.Value, out var tpl)
            || tpl.MatchesDate(member.ScheduleStartDate, occurrenceDate);
    }

    private static bool HasMatchingAssignment(
        GroupMemberId memberId,
        List<GroupMemberAssignment> assignments,
        Guid locationId,
        Guid scheduleId
    )
    {
        return assignments.Exists(a =>
            a.GroupMemberId == memberId
            && (!a.LocationId.HasValue || a.LocationId == locationId)
            && (!a.ScheduleId.HasValue || a.ScheduleId == scheduleId)
        );
    }

    private sealed record PopulationContext(
        Dictionary<GroupLocationId, GroupLocation> LocationsById,
        List<GroupLocationScheduleConfig> Configs,
        List<GroupMember> Members,
        Dictionary<GroupMemberScheduleTemplateId, GroupMemberScheduleTemplate> Templates,
        List<GroupMemberAssignment> Assignments
    );
}
