using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Queries live room occupancy, threshold states, and participant rosters for the Room/Facility Manager UI.</summary>
/// <param name="ConfigurationId">Check-in configuration identifier.</param>
/// <param name="ScheduleId">Optional schedule filter.</param>
/// <param name="AreaId">Optional check-in area filter.</param>
/// <param name="LocationId">Optional room/location filter.</param>
/// <param name="ServiceDate">Optional local service date (defaults to current UTC date).</param>
/// <param name="IncludeCheckedOut">Whether to include checked-out entries in room rosters.</param>
public sealed record GetLiveRoomRosterQuery(
    Guid ConfigurationId,
    Guid? ScheduleId = null,
    Guid? AreaId = null,
    Guid? LocationId = null,
    DateOnly? ServiceDate = null,
    bool IncludeCheckedOut = false
) : IQuery<LiveRoomRosterResponse>;
