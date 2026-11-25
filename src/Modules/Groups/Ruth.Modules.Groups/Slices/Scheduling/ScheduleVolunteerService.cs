using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.Modules.Groups.Slices.Requirements;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class ScheduleVolunteerService(
    GroupsDbContext dbContext,
    GroupRequirementEvaluatorService requirementEvaluator,
    IClock clock
)
{
    public async Task<Result<ScheduledVolunteerResponse>> ScheduleAsync(
        ScheduleVolunteerCommand command,
        CancellationToken cancellationToken
    )
    {
        var loaded = await LoadSchedulableGroupAsync(
            GroupId.From(command.GroupId),
            cancellationToken
        );
        if (loaded.IsFailure)
        {
            return Result.Failure<ScheduledVolunteerResponse>(loaded.Errors);
        }

        var (group, groupType) = loaded.Value;
        var conflicts = await EvaluateConflictsAsync(group, groupType, command, cancellationToken);
        if (!command.ForceOverrideConflicts && conflicts.HasAnyBlockingConflict)
        {
            return GroupsErrors.VolunteerConflict;
        }

        var occurrence = await GetOrCreateOccurrenceAsync(
            group,
            command.LocationId,
            command.ScheduleId,
            command.OccurrenceDate,
            cancellationToken
        );

        if (await ExceedsMaxCapacityAsync(group, occurrence, command, cancellationToken))
        {
            return GroupsErrors.VolunteerConflict;
        }

        var autoConfirm =
            groupType.ScheduleConfirmationLogic == ScheduleConfirmationLogic.AutoAccept;
        var attendance = occurrence.ScheduleVolunteer(
            command.PersonId,
            group.BranchId,
            command.ScheduledByPersonId,
            autoConfirm,
            clock.UtcNow
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var locationName = group
            .Locations.FirstOrDefault(l => l.LocationId == command.LocationId)
            ?.LocationName;
        return Result.Success(
            ToScheduledResponse(
                attendance,
                occurrence,
                group.Name,
                locationName,
                conflicts.HasBlackout,
                conflicts.HasDoubleBooking,
                conflicts.MeetsRequirements
            )
        );
    }

    public static ScheduledVolunteerResponse ToScheduledResponse(
        Attendance attendance,
        AttendanceOccurrence occurrence,
        string groupName,
        string? locationName,
        bool hasBlackout,
        bool hasDoubleBooking,
        bool meetsRequirements
    )
    {
        return new ScheduledVolunteerResponse(
            attendance.Id.Value,
            occurrence.Id.Value,
            occurrence.GroupId?.Value ?? Guid.Empty,
            groupName,
            occurrence.LocationId,
            locationName,
            occurrence.ScheduleId,
            occurrence.OccurrenceDate,
            attendance.PersonId ?? Guid.Empty,
            attendance.RequestedToAttend ?? false,
            attendance.ScheduledToAttend ?? false,
            attendance.Rsvp,
            attendance.RsvpDateTimeUtc,
            attendance.RsvpToken,
            attendance.DeclineReasonValueId,
            attendance.DeclineReasonNote,
            attendance.ScheduleConfirmationSent ?? false,
            attendance.ScheduleReminderSent ?? false,
            hasBlackout,
            hasDoubleBooking,
            meetsRequirements
        );
    }

    private async Task<Result<(Group Group, GroupType GroupType)>> LoadSchedulableGroupAsync(
        GroupId groupId,
        CancellationToken cancellationToken
    )
    {
        var group = await dbContext
            .Groups.Include(static g => g.Locations)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .FirstOrDefaultAsync(gt => gt.Id == group.GroupTypeId, cancellationToken);
        return groupType is null ? (Result<(Group, GroupType)>)GroupsErrors.GroupTypeNotFound
            : !groupType.IsSchedulingEnabled || group.DisableScheduling
                ? GroupsErrors.SchedulingDisabled
            : Result.Success((group, groupType));
    }

    private async Task<bool> ExceedsMaxCapacityAsync(
        Group group,
        AttendanceOccurrence occurrence,
        ScheduleVolunteerCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.ForceOverrideConflicts)
        {
            return false;
        }

        var maxCapacity = await GetMaximumCapacityAsync(
            group,
            command.LocationId,
            command.ScheduleId,
            cancellationToken
        );
        var currentActiveCount = occurrence.Attendances.Count(a =>
            a.PersonId != command.PersonId && a.RequestedToAttend == true && a.Rsvp != RsvpStatus.No
        );
        return ServingScheduleEvaluator.WouldExceedMaximumCapacity(currentActiveCount, maxCapacity);
    }

    private async Task<ConflictSummary> EvaluateConflictsAsync(
        Group group,
        GroupType groupType,
        ScheduleVolunteerCommand command,
        CancellationToken cancellationToken
    )
    {
        var groupTypeExcluded = await dbContext.GroupScheduleExclusions.AnyAsync(
            e =>
                e.GroupTypeId == groupType.Id
                && command.OccurrenceDate >= e.StartDate
                && command.OccurrenceDate <= e.EndDate,
            cancellationToken
        );
        var hasBlackout = await dbContext.PersonScheduleExclusions.AnyAsync(
            e =>
                e.PersonId == command.PersonId
                && command.OccurrenceDate >= e.StartDate
                && command.OccurrenceDate <= e.EndDate
                && (e.GroupId == null || e.GroupId == group.Id),
            cancellationToken
        );
        var hasDoubleBooking = await HasDoubleBookingAsync(group.Id, command, cancellationToken);
        var meetsRequirements = await CheckRequirementsForSchedulingAsync(
            group,
            command.PersonId,
            cancellationToken
        );

        return new ConflictSummary(
            groupTypeExcluded,
            hasBlackout,
            hasDoubleBooking,
            meetsRequirements
        );
    }

    private async Task<bool> HasDoubleBookingAsync(
        GroupId groupId,
        ScheduleVolunteerCommand command,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .AttendanceOccurrences.AsNoTracking()
            .Where(o =>
                o.OccurrenceDate == command.OccurrenceDate
                && o.ScheduleId == command.ScheduleId
                && (o.GroupId != groupId || o.LocationId != command.LocationId)
            )
            .SelectMany(static o => o.Attendances)
            .AnyAsync(
                a =>
                    a.PersonId == command.PersonId
                    && a.RequestedToAttend == true
                    && a.Rsvp != RsvpStatus.No,
                cancellationToken
            );
    }

    private async Task<bool> CheckRequirementsForSchedulingAsync(
        Group group,
        Guid personId,
        CancellationToken cancellationToken
    )
    {
        if (!group.SchedulingMustMeetRequirements)
        {
            return true;
        }

        var member = await dbContext
            .GroupMembers.AsNoTracking()
            .FirstOrDefaultAsync(
                m =>
                    m.GroupId == group.Id
                    && m.PersonId == personId
                    && !m.IsArchived
                    && m.Status == GroupMemberStatus.Active,
                cancellationToken
            );
        return member is not null
            && await requirementEvaluator.MemberMeetsAllRequirementsForSchedulingAsync(
                member,
                group,
                cancellationToken
            );
    }

    private async Task<AttendanceOccurrence> GetOrCreateOccurrenceAsync(
        Group group,
        Guid? locationId,
        Guid? scheduleId,
        DateOnly occurrenceDate,
        CancellationToken cancellationToken
    )
    {
        var occurrence = await dbContext
            .AttendanceOccurrences.Include(static o => o.Attendances)
            .FirstOrDefaultAsync(
                o =>
                    o.GroupId == group.Id
                    && o.LocationId == locationId
                    && o.ScheduleId == scheduleId
                    && o.OccurrenceDate == occurrenceDate,
                cancellationToken
            );
        if (occurrence is not null)
        {
            return occurrence;
        }

        occurrence = AttendanceOccurrence.Create(
            group.Id,
            group.GroupTypeId,
            locationId,
            scheduleId,
            occurrenceDate
        );
        _ = dbContext.AttendanceOccurrences.Add(occurrence);
        return occurrence;
    }

    private async Task<int?> GetMaximumCapacityAsync(
        Group group,
        Guid? locationId,
        Guid? scheduleId,
        CancellationToken cancellationToken
    )
    {
        if (!locationId.HasValue || !scheduleId.HasValue)
        {
            return null;
        }

        var groupLocation = group.Locations.FirstOrDefault(l => l.LocationId == locationId.Value);
        return groupLocation is null
            ? null
            : await dbContext
                .LocationScheduleConfigs.AsNoTracking()
                .Where(c =>
                    c.GroupLocationId == groupLocation.Id && c.ScheduleId == scheduleId.Value
                )
                .Select(static c => c.MaximumCapacity)
                .FirstOrDefaultAsync(cancellationToken);
    }

    private sealed record ConflictSummary(
        bool GroupTypeExcluded,
        bool HasBlackout,
        bool HasDoubleBooking,
        bool MeetsRequirements
    )
    {
        public bool HasAnyBlockingConflict =>
            GroupTypeExcluded || HasBlackout || HasDoubleBooking || !MeetsRequirements;
    }
}
