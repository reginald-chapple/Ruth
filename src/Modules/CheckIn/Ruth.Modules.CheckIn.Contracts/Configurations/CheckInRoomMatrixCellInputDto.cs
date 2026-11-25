namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CheckInRoomMatrixCellInputDto in the CheckIn module.</summary>
public sealed record CheckInRoomMatrixCellInputDto(
    Guid LocationId,
    string LocationName,
    IReadOnlyList<Guid> ScheduleIds,
    int? SoftCapacity = null,
    int? FirmCapacity = null,
    int PriorityOrder = 0,
    bool IsOpenForCheckIn = true,
    bool IsOverflowRoom = false
);
