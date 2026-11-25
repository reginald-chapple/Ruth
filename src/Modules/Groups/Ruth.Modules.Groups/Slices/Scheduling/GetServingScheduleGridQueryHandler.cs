using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class GetServingScheduleGridQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetServingScheduleGridQuery, ServingScheduleGridResponse>
{
    public async Task<Result<ServingScheduleGridResponse>> HandleAsync(
        GetServingScheduleGridQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (query.EndDate < query.StartDate)
        {
            return GroupsErrors.InvalidDateRange;
        }

        var groupId = GroupId.From(query.GroupId);
        var group = await dbContext
            .Groups.AsNoTracking()
            .Include(static g => g.Locations)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .Include(static gt => gt.Roles)
            .FirstOrDefaultAsync(gt => gt.Id == group.GroupTypeId, cancellationToken);
        return groupType is null
            ? (Result<ServingScheduleGridResponse>)GroupsErrors.GroupTypeNotFound
            : Result.Success(await BuildGridAsync(group, groupType, query, cancellationToken));
    }

    private async Task<ServingScheduleGridResponse> BuildGridAsync(
        Group group,
        Domain.GroupTypes.GroupType groupType,
        GetServingScheduleGridQuery query,
        CancellationToken cancellationToken
    )
    {
        var (occurrences, configs, groupExclusions) = await LoadSlotDataAsync(
            group,
            query,
            cancellationToken
        );
        var (members, personExclusions, unmetIds, templates) = await LoadCandidateDataAsync(
            group.Id,
            query,
            cancellationToken
        );

        var slots = ServingScheduleGridBuilder.BuildSlots(
            group,
            occurrences,
            configs,
            groupExclusions,
            personExclusions
        );
        var candidates = ServingScheduleGridBuilder.BuildCandidates(
            group,
            groupType,
            members,
            unmetIds,
            templates,
            personExclusions,
            groupExclusions,
            query.StartDate
        );

        return new ServingScheduleGridResponse(
            group.Id.Value,
            group.Name,
            query.StartDate,
            query.EndDate,
            slots,
            candidates
        );
    }

    private async Task<(
        List<Domain.Attendances.AttendanceOccurrence>,
        List<Domain.Scheduling.GroupLocationScheduleConfig>,
        List<Domain.Scheduling.GroupScheduleExclusion>
    )> LoadSlotDataAsync(
        Group group,
        GetServingScheduleGridQuery query,
        CancellationToken cancellationToken
    )
    {
        var occurrences = await dbContext
            .AttendanceOccurrences.AsNoTracking()
            .Include(static o => o.Attendances)
            .Where(o =>
                o.GroupId == group.Id
                && o.OccurrenceDate >= query.StartDate
                && o.OccurrenceDate <= query.EndDate
            )
            .OrderBy(static o => o.OccurrenceDate)
            .ToListAsync(cancellationToken);

        var glIds = group.Locations.Select(static l => l.Id).ToArray();
        var configs = await dbContext
            .LocationScheduleConfigs.AsNoTracking()
            .Where(c => glIds.Contains(c.GroupLocationId))
            .ToListAsync(cancellationToken);

        var groupExclusions = await dbContext
            .GroupScheduleExclusions.AsNoTracking()
            .Where(e =>
                e.GroupTypeId == group.GroupTypeId
                && e.EndDate >= query.StartDate
                && e.StartDate <= query.EndDate
            )
            .ToListAsync(cancellationToken);

        return (occurrences, configs, groupExclusions);
    }

    private async Task<(
        List<GroupMember>,
        List<Domain.Scheduling.PersonScheduleExclusion>,
        HashSet<GroupMemberId>,
        Dictionary<
            Domain.Scheduling.GroupMemberScheduleTemplateId,
            Domain.Scheduling.GroupMemberScheduleTemplate
        >
    )> LoadCandidateDataAsync(
        GroupId groupId,
        GetServingScheduleGridQuery query,
        CancellationToken cancellationToken
    )
    {
        var members = await dbContext
            .GroupMembers.AsNoTracking()
            .Where(m =>
                m.GroupId == groupId && !m.IsArchived && m.Status == GroupMemberStatus.Active
            )
            .ToListAsync(cancellationToken);

        var personIds = members.Select(static m => m.PersonId).Distinct().ToArray();
        var personExclusions = await dbContext
            .PersonScheduleExclusions.AsNoTracking()
            .Where(e =>
                personIds.Contains(e.PersonId)
                && e.EndDate >= query.StartDate
                && e.StartDate <= query.EndDate
            )
            .ToListAsync(cancellationToken);

        var memberIds = members.Select(static m => m.Id).ToArray();
        var unmetIds = new HashSet<GroupMemberId>(
            await dbContext
                .GroupMemberRequirements.AsNoTracking()
                .Where(mr =>
                    memberIds.Contains(mr.GroupMemberId)
                    && (
                        mr.State == MeetsGroupRequirement.NotMet
                        || mr.State == MeetsGroupRequirement.Error
                    )
                )
                .Select(static mr => mr.GroupMemberId)
                .ToListAsync(cancellationToken)
        );
        var templates = await dbContext
            .ScheduleTemplates.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

        return (members, personExclusions, unmetIds, templates);
    }
}
