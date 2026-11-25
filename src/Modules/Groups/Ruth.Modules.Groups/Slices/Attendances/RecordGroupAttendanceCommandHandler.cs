using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Attendances;

internal sealed class RecordGroupAttendanceCommandHandler(GroupsDbContext dbContext, IClock clock)
    : ICommandHandler<RecordGroupAttendanceCommand, AttendanceOccurrenceDetailResponse>
{
    public async Task<Result<AttendanceOccurrenceDetailResponse>> HandleAsync(
        RecordGroupAttendanceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var validated = await LoadAndValidateGroupAsync(command, cancellationToken);
        if (validated.IsFailure)
        {
            return Result.Failure<AttendanceOccurrenceDetailResponse>(validated.Errors);
        }

        var (group, groupType) = validated.Value;
        var occurrence = await GetOrCreateOccurrenceAsync(group, command, cancellationToken);
        var metaResult = occurrence.UpdateOccurrenceMetadata(
            command.DidNotOccur,
            command.AnonymousAttendanceCount,
            command.Notes,
            command.Name
        );
        if (metaResult.IsFailure)
        {
            return Result.Failure<AttendanceOccurrenceDetailResponse>(metaResult.Errors);
        }

        if (command.Entries is { Count: > 0 })
        {
            var entryResult = await ApplyRosterEntriesAsync(
                occurrence,
                group,
                groupType.CountsTowardHeadlineAttendance,
                command,
                cancellationToken
            );
            if (entryResult.IsFailure)
            {
                return Result.Failure<AttendanceOccurrenceDetailResponse>(entryResult.Errors);
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(AttendanceMapper.ToDetail(occurrence, group.Name));
    }

    private async Task<Result<(Group Group, GroupType GroupType)>> LoadAndValidateGroupAsync(
        RecordGroupAttendanceCommand command,
        CancellationToken cancellationToken
    )
    {
        var groupId = GroupId.From(command.GroupId);
        var group = await dbContext
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .FirstOrDefaultAsync(gt => gt.Id == group.GroupTypeId, cancellationToken);
        return groupType is null ? (Result<(Group, GroupType)>)GroupsErrors.GroupTypeNotFound
            : !groupType.TakesAttendance ? GroupsErrors.AttendanceNotEnabled
            : groupType.GroupAttendanceRequiresLocation && !command.LocationId.HasValue
                ? GroupsErrors.AttendanceLocationRequired
            : groupType.GroupAttendanceRequiresSchedule && !command.ScheduleId.HasValue
                ? GroupsErrors.AttendanceScheduleRequired
            : Result.Success((group, groupType));
    }

    private async Task<AttendanceOccurrence> GetOrCreateOccurrenceAsync(
        Group group,
        RecordGroupAttendanceCommand command,
        CancellationToken cancellationToken
    )
    {
        var occurrence = await dbContext
            .AttendanceOccurrences.Include(static o => o.Attendances)
            .FirstOrDefaultAsync(
                o =>
                    o.GroupId == group.Id
                    && o.LocationId == command.LocationId
                    && o.ScheduleId == command.ScheduleId
                    && o.OccurrenceDate == command.OccurrenceDate,
                cancellationToken
            );
        if (occurrence is not null)
        {
            return occurrence;
        }

        occurrence = AttendanceOccurrence.Create(
            group.Id,
            group.GroupTypeId,
            command.LocationId,
            command.ScheduleId,
            command.OccurrenceDate,
            command.Name
        );
        _ = dbContext.AttendanceOccurrences.Add(occurrence);
        return occurrence;
    }

    private async Task<Result> ApplyRosterEntriesAsync(
        AttendanceOccurrence occurrence,
        Group group,
        bool countsTowardHeadlineAttendance,
        RecordGroupAttendanceCommand command,
        CancellationToken cancellationToken
    )
    {
        var entries = command.Entries!;
        Guid[] personIds = [.. entries.Select(static e => e.PersonId).Distinct()];
        var previouslyAttended = new HashSet<Guid>(
            await dbContext
                .Attendances.AsNoTracking()
                .Where(a =>
                    a.PersonId.HasValue
                    && personIds.Contains(a.PersonId.Value)
                    && a.DidAttend == true
                    && a.OccurrenceId != occurrence.Id
                )
                .Select(static a => a.PersonId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken)
        );

        var nowUtc = clock.UtcNow;
        foreach (var entry in entries)
        {
            var isFirstTime = !previouslyAttended.Contains(entry.PersonId);
            var recorded = occurrence.RecordPersonAttendance(
                entry.PersonId,
                group.BranchId,
                entry.DidAttend,
                isFirstTime,
                entry.Note,
                command.CheckedInByPersonId,
                countsTowardHeadlineAttendance,
                nowUtc
            );
            if (recorded.IsFailure)
            {
                return recorded;
            }
        }

        return Result.Success();
    }
}
