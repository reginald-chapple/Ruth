namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Complete read projection for an assistance, scholarship, fee-waiver, or micro-grant case.</summary>
public sealed record AssistanceCaseResponse(
    Guid Id,
    string CaseNumber,
    Guid ProgramId,
    string ProgramName,
    AssistanceProgramCategory ProgramCategory,
    Guid ApplicantPersonId,
    Guid? HouseholdId,
    Guid? AssignedCaseworkerPersonId,
    Guid? BranchId,
    AssistanceCaseStatus Status,
    string Summary,
    string? RequestDetails,
    string? DecisionNotes,
    string CurrencyCode,
    decimal RequestedAmount,
    decimal ApprovedAmount,
    decimal DisbursedAmount,
    decimal RemainingApprovedAmount,
    IReadOnlyList<AssistanceDocumentResponse> Documents,
    IReadOnlyList<AssistanceDisbursementResponse> Disbursements,
    IReadOnlyDictionary<string, string?> Attributes,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    DateTimeOffset? ClosedAtUtc
);
