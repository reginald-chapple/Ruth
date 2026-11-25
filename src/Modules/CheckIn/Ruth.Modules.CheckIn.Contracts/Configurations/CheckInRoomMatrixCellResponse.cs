namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CheckInRoomMatrixCellResponse in the CheckIn module.</summary>
public sealed record CheckInRoomMatrixCellResponse(
    Guid Id,
    Guid LocationId,
    string LocationName,
    IReadOnlyList<Guid> ScheduleIds,
    int? SoftCapacity,
    int? FirmCapacity,
    int PriorityOrder,
    bool IsOpenForCheckIn,
    bool IsOverflowRoom
);
