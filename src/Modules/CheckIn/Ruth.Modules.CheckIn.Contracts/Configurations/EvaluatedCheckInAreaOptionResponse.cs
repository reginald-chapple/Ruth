namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type EvaluatedCheckInAreaOptionResponse in the CheckIn module.</summary>
public sealed record EvaluatedCheckInAreaOptionResponse(
    Guid AreaId,
    string AreaName,
    Guid? LinkedGroupId,
    bool IsEligible,
    IReadOnlyList<string> IneligibilityReasons,
    bool PrintBadge,
    bool IssueSecurityPickupCode,
    bool SupportsAccommodations,
    Guid? RecommendedLocationId,
    string? RecommendedLocationName,
    IReadOnlyList<EvaluatedCheckInRoomOptionResponse> CandidateRooms
);
