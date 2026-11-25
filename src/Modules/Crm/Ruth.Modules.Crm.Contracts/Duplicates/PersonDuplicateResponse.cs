namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Represents a potential duplicate pair of person records scored by the CRM duplicate engine.</summary>
public sealed record PersonDuplicateResponse(
    Guid Id,
    Guid Person1Id,
    string Person1FullName,
    string? Person1Email,
    Guid Person2Id,
    string Person2FullName,
    string? Person2Email,
    int Score,
    int Capacity,
    int TotalCapacity,
    decimal ConfidenceScore,
    string ScoreDetail,
    bool IsConfirmedAsNotDuplicate,
    bool IgnoreUntilScoreChanges,
    DateTimeOffset LastScannedOnUtc
);
