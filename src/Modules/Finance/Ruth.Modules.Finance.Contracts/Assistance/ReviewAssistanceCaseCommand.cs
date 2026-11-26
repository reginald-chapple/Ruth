using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Command to place a case under review, approve an award ceiling, or deny an assistance case.</summary>
public sealed record ReviewAssistanceCaseCommand(
    Guid CaseId,
    AssistanceCaseStatus DecisionStatus,
    decimal? ApprovedAmount = null,
    Guid? ReviewerPersonId = null,
    Guid? AssignedCaseworkerPersonId = null,
    string? DecisionNotes = null
) : ICommand<AssistanceCaseResponse>;
