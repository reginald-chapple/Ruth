namespace Ruth.Modules.Crm.Contracts.Assessments;

/// <summary>Represents a person's pending or completed assessment record.</summary>
public sealed record AssessmentResponse(
    Guid Id,
    Guid PersonId,
    Guid AssessmentTypeId,
    string AssessmentTypeKey,
    string AssessmentTypeTitle,
    Guid? RequesterPersonId,
    DateTimeOffset? RequestedOnUtc,
    DateOnly? RequestedDueDate,
    AssessmentStatus Status,
    DateTimeOffset? CompletedOnUtc,
    string? ResultDataJson,
    string? SummaryText,
    bool IsExpired
);
