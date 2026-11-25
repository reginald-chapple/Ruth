using Ruth.Modules.Crm.Contracts.People;

namespace Ruth.Modules.Crm.Contracts.Badges;

/// <summary>Read-only context supplied to modular <see cref="IPersonBadgeEvaluator" /> plugins.</summary>
public sealed record PersonBadgeEvaluationContext(
    PersonDetailResponse Person,
    int CompletedAssessmentsCount,
    string? LatestAssessmentSummary,
    string? LatestBackgroundCheckStatus,
    bool? LatestBackgroundCheckRecordFound,
    DateTimeOffset EvaluatedOnUtc
);
