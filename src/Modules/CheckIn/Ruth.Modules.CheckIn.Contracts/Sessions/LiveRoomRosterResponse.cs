namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Live Room/Facility Manager snapshot across areas and rooms.</summary>
/// <param name="ConfigurationId">Configuration identifier.</param>
/// <param name="ConfigurationName">Configuration display name.</param>
/// <param name="ServiceDate">Evaluated local service date.</param>
/// <param name="TotalCheckedIn">Total active checked-in headcount across matching rooms.</param>
/// <param name="TotalCheckedOut">Total checked-out count across matching rooms.</param>
/// <param name="Rooms">Per-room live occupancy and participant rosters.</param>
public sealed record LiveRoomRosterResponse(
    Guid ConfigurationId,
    string ConfigurationName,
    DateOnly ServiceDate,
    int TotalCheckedIn,
    int TotalCheckedOut,
    IReadOnlyList<LiveRoomSummaryDto> Rooms
);
