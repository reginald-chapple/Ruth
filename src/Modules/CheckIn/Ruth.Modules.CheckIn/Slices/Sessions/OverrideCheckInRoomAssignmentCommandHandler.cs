using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class OverrideCheckInRoomAssignmentCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<OverrideCheckInRoomAssignmentCommand, CheckedInEntryReceiptDto>
{
    public async Task<Result<CheckedInEntryReceiptDto>> HandleAsync(
        OverrideCheckInRoomAssignmentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var loaded = await LoadSessionAndEntryAsync(command, cancellationToken);
        if (loaded.IsFailure)
        {
            return Result.Failure<CheckedInEntryReceiptDto>(loaded.Errors);
        }

        var (session, entry) = loaded.Value;
        var targetResult = await ResolveTargetRoomAsync(
            session.CheckInConfigurationId,
            command.TargetAreaId,
            command.TargetLocationId,
            cancellationToken
        );
        if (targetResult.IsFailure)
        {
            return Result.Failure<CheckedInEntryReceiptDto>(targetResult.Errors);
        }

        var requiresCapacityOverride = await RequiresCapacityOverrideAsync(
            session.CheckInConfigurationId,
            entry.Id,
            entry.ServiceDate,
            entry.ScheduleId,
            targetResult.Value.Room,
            cancellationToken
        );
        if (requiresCapacityOverride && !command.AllowCapacityOverride)
        {
            return Result.Failure<CheckedInEntryReceiptDto>(
                CheckInErrors.NoAvailableRoomForCheckIn
            );
        }

        var reassigned = session.ReassignEntryRoom(
            entry.Id,
            targetResult.Value.Area.Id,
            targetResult.Value.Area.Name,
            targetResult.Value.Room.LocationId,
            targetResult.Value.Room.LocationName,
            command.OverrideByPersonId,
            command.Reason,
            requiresCapacityOverride
        );
        if (reassigned.IsFailure)
        {
            return Result.Failure<CheckedInEntryReceiptDto>(reassigned.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(CheckInSessionMapper.ToEntryReceipt(reassigned.Value));
    }

    private async Task<
        Result<(
            Domain.Sessions.AttendanceCheckInSession Session,
            Domain.Sessions.CheckInAttendanceEntry Entry
        )>
    > LoadSessionAndEntryAsync(
        OverrideCheckInRoomAssignmentCommand command,
        CancellationToken cancellationToken
    )
    {
        var sessionId = AttendanceCheckInSessionId.From(command.SessionId);
        var session = await dbContext
            .AttendanceCheckInSessions.Include(static s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<(
                Domain.Sessions.AttendanceCheckInSession,
                Domain.Sessions.CheckInAttendanceEntry
            )>(CheckInErrors.SessionNotFound);
        }

        var entryId = CheckInAttendanceEntryId.From(command.EntryId);
        var entry = session.Entries.FirstOrDefault(e => e.Id == entryId);
        return entry is null
            ? Result.Failure<(
                Domain.Sessions.AttendanceCheckInSession,
                Domain.Sessions.CheckInAttendanceEntry
            )>(CheckInErrors.EntryNotFound)
            : Result.Success((session, entry));
    }

    private async Task<Result<(CheckInArea Area, CheckInAreaLocation Room)>> ResolveTargetRoomAsync(
        CheckInConfigurationId configurationId,
        Guid targetAreaGuid,
        Guid targetLocationId,
        CancellationToken cancellationToken
    )
    {
        var config = await dbContext
            .CheckInConfigurations.AsNoTracking()
            .Include(static c => c.Areas)
                .ThenInclude(static a => a.Rooms)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken);
        if (config is null)
        {
            return Result.Failure<(CheckInArea, CheckInAreaLocation)>(
                CheckInErrors.ConfigurationNotFound
            );
        }

        var targetAreaId = CheckInAreaId.From(targetAreaGuid);
        var targetArea = config.Areas.FirstOrDefault(a => a.Id == targetAreaId);
        if (targetArea is null)
        {
            return Result.Failure<(CheckInArea, CheckInAreaLocation)>(CheckInErrors.AreaNotFound);
        }

        var targetRoom = targetArea.Rooms.FirstOrDefault(r => r.LocationId == targetLocationId);
        return targetRoom is null
            ? Result.Failure<(CheckInArea, CheckInAreaLocation)>(CheckInErrors.RoomNotFound)
            : Result.Success((targetArea, targetRoom));
    }

    private async Task<bool> RequiresCapacityOverrideAsync(
        CheckInConfigurationId configurationId,
        CheckInAttendanceEntryId entryId,
        DateOnly serviceDate,
        Guid scheduleId,
        CheckInAreaLocation targetRoom,
        CancellationToken cancellationToken
    )
    {
        var activeInTarget = await dbContext
            .CheckInAttendanceEntries.AsNoTracking()
            .CountAsync(
                e =>
                    e.CheckInConfigurationId == configurationId
                    && e.ServiceDate == serviceDate
                    && e.ScheduleId == scheduleId
                    && e.LocationId == targetRoom.LocationId
                    && e.Status == CheckInEntryStatus.CheckedIn
                    && e.Id != entryId,
                cancellationToken
            );

        var status = RoomBalancingDomainService.EvaluateThresholdStatus(
            targetRoom,
            scheduleId,
            activeInTarget
        );
        return status is RoomThresholdStatus.Closed or RoomThresholdStatus.AtFirmCapacity;
    }
}
