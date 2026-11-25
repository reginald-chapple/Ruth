using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Assessments;

/// <summary>Command to complete an assessment and persist structured JSON result data.</summary>
public sealed record CompleteAssessmentCommand(
    Guid AssessmentId,
    string ResultDataJson,
    string? SummaryText = null
) : ICommand<AssessmentResponse>;
