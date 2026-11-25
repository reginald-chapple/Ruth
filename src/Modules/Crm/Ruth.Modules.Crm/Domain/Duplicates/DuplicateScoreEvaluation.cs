namespace Ruth.Modules.Crm.Domain.Duplicates;

/// <summary>Result of scoring two <see cref="DuplicateCandidateProfile" /> records for duplicate likelihood.</summary>
public sealed record DuplicateScoreEvaluation(
    bool IsCandidateMatch,
    int Score,
    int Capacity,
    int TotalCapacity,
    decimal ConfidenceScore,
    string ScoreDetail
);
