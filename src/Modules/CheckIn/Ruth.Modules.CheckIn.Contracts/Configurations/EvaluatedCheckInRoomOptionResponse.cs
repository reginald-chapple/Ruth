namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type EvaluatedCheckInRoomOptionResponse in the CheckIn module.</summary>
public sealed record EvaluatedCheckInRoomOptionResponse(
    Guid LocationId,
    string LocationName,
    int CurrentOccupancy,
    int? SoftCapacity,
    int? FirmCapacity,
    bool IsOverflowRoom,
    RoomThresholdStatus ThresholdStatus,
    decimal OccupancyRatio,
    bool IsRecommended
);
