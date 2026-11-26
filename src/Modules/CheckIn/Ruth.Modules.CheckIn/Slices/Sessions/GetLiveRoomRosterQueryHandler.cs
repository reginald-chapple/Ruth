using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Domain.Sessions;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class GetLiveRoomRosterQueryHandler(CheckInDbContext dbContext, IClock clock)
    : IQueryHandler<GetLiveRoomRosterQuery, LiveRoomRosterResponse>
{
    public async Task<Result<LiveRoomRosterResponse>> HandleAsync(
        GetLiveRoomRosterQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var configId = CheckInConfigurationId.From(query.ConfigurationId);
        var config = await dbContext
            .CheckInConfigurations.AsNoTracking()
            .Include(static c => c.Areas)
                .ThenInclude(static a => a.Rooms)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == configId, cancellationToken);
        if (config is null)
        {
            return Result.Failure<LiveRoomRosterResponse>(CheckInErrors.ConfigurationNotFound);
        }

        var serviceDate = query.ServiceDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var entries = await LoadFilteredEntriesAsync(
            configId,
            query,
            serviceDate,
            cancellationToken
        );
        var rooms = BuildRoomSummaries(config, query, entries);

        var totalIn = entries.Count(static e => e.Status == CheckInEntryStatus.CheckedIn);
        var totalOut = entries.Count(static e => e.Status == CheckInEntryStatus.CheckedOut);

        return Result.Success(
            new LiveRoomRosterResponse(
                config.Id.Value,
                config.Name,
                serviceDate,
                totalIn,
                totalOut,
                rooms
            )
        );
    }

    private Task<List<CheckInAttendanceEntry>> LoadFilteredEntriesAsync(
        CheckInConfigurationId configId,
        GetLiveRoomRosterQuery query,
        DateOnly serviceDate,
        CancellationToken cancellationToken
    )
    {
        var q = dbContext
            .CheckInAttendanceEntries.AsNoTracking()
            .Where(e => e.CheckInConfigurationId == configId && e.ServiceDate == serviceDate);

        if (query.ScheduleId is { } sid && sid != Guid.Empty)
        {
            q = q.Where(e => e.ScheduleId == sid);
        }

        if (query.AreaId is { } aid && aid != Guid.Empty)
        {
            var areaId = CheckInAreaId.From(aid);
            q = q.Where(e => e.CheckInAreaId == areaId);
        }

        if (query.LocationId is { } lid && lid != Guid.Empty)
        {
            q = q.Where(e => e.LocationId == lid);
        }

        return q.OrderBy(static e => e.PersonFullName).ToListAsync(cancellationToken);
    }

    private static List<LiveRoomSummaryDto> BuildRoomSummaries(
        CheckInConfiguration config,
        GetLiveRoomRosterQuery query,
        List<CheckInAttendanceEntry> entries
    )
    {
        var output = new List<LiveRoomSummaryDto>();
        var areas = config
            .Areas.Where(a => !query.AreaId.HasValue || a.Id.Value == query.AreaId.Value)
            .OrderBy(static a => a.Order);

        foreach (var area in areas)
        {
            var rooms = area
                .Rooms.Where(r =>
                    !query.LocationId.HasValue || r.LocationId == query.LocationId.Value
                )
                .OrderBy(static r => r.PriorityOrder);

            foreach (var room in rooms)
            {
                output.Add(BuildSingleRoomSummary(area, room, query, entries));
            }
        }

        return output;
    }

    private static LiveRoomSummaryDto BuildSingleRoomSummary(
        CheckInArea area,
        CheckInAreaLocation room,
        GetLiveRoomRosterQuery query,
        List<CheckInAttendanceEntry> entries
    )
    {
        var cellEntries = entries
            .Where(e => e.CheckInAreaId == area.Id && e.LocationId == room.LocationId)
            .ToList();
        var activeCount = cellEntries.Count(static e => e.Status == CheckInEntryStatus.CheckedIn);
        var status = ComputeLiveStatus(room, query.ScheduleId, activeCount);

        var roster = cellEntries
            .Where(e => query.IncludeCheckedOut || e.Status == CheckInEntryStatus.CheckedIn)
            .Select(static e => new LiveRoomParticipantDto(
                e.SessionId.Value,
                e.Id.Value,
                e.PersonId,
                e.PersonFullName,
                e.ScheduleId,
                e.Status,
                e.SecurityPickupCode,
                e.AttendanceNote,
                e.IsCapacityOverride,
                e.IsEligibilityOverride,
                e.CheckedInOnUtc,
                e.CheckedOutOnUtc
            ))
            .ToList();

        return new LiveRoomSummaryDto(
            area.Id.Value,
            area.Name,
            room.LocationId,
            room.LocationName,
            room.IsOpenForCheckIn,
            room.IsOverflowRoom,
            activeCount,
            room.SoftCapacity,
            room.FirmCapacity,
            status,
            roster
        );
    }

    private static RoomThresholdStatus ComputeLiveStatus(
        CheckInAreaLocation room,
        Guid? scheduleId,
        int activeCount
    )
    {
        return scheduleId is { } sid && sid != Guid.Empty
                ? RoomBalancingDomainService.EvaluateThresholdStatus(room, sid, activeCount)
            : !room.IsOpenForCheckIn ? RoomThresholdStatus.Closed
            : room.FirmCapacity is { } firm && activeCount >= firm
                ? RoomThresholdStatus.AtFirmCapacity
            : room.SoftCapacity is { } soft && activeCount >= soft
                ? RoomThresholdStatus.AtSoftCapacity
            : RoomThresholdStatus.Open;
    }
}
