namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Recorded disbursement or invoice fee-waiver credit under an approved assistance case.</summary>
public sealed record AssistanceDisbursementResponse(
    Guid Id,
    Guid CaseId,
    AssistanceDisbursementMethod DisbursementMethod,
    decimal Amount,
    string CurrencyCode,
    string PayeeName,
    Guid? TargetInvoiceId,
    string? ReferenceNumber,
    string? Notes,
    Guid? ApprovedByPersonId,
    DateTimeOffset DisbursedAtUtc
);
