using Ruth.Modules.CheckIn.Contracts.Configurations;

namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Real-time capacity and roster state for a single check-in room cell.</summary>
/// <param name="AreaId">Owning check-in area identifier.</param>
/// <param name="AreaName">Owning check-in area name.</param>
/// <param name="LocationId">Room location identifier.</param>
/// <param name="LocationName">Room display label.</param>
/// <param name="IsOpenForCheckIn">Whether the room is currently open.</param>
/// <param name="IsOverflowRoom">Whether the room is designated as overflow.</param>
/// <param name="ActiveOccupancy">Current checked-in participant count.</param>
/// <param name="SoftCapacity">Optional soft capacity threshold.</param>
/// <param name="FirmCapacity">Optional firm capacity ceiling.</param>
/// <param name="ThresholdStatus">Evaluated threshold state.</param>
/// <param name="Participants">Participants assigned to this room on the service date.</param>
public sealed record LiveRoomSummaryDto(
    Guid AreaId,
    string AreaName,
    Guid LocationId,
    string LocationName,
    bool IsOpenForCheckIn,
    bool IsOverflowRoom,
    int ActiveOccupancy,
    int? SoftCapacity,
    int? FirmCapacity,
    RoomThresholdStatus ThresholdStatus,
    IReadOnlyList<LiveRoomParticipantDto> Participants
);
