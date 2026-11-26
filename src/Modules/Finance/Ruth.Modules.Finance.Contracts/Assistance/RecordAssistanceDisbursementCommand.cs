using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Command to record a disbursement or internal invoice fee-waiver credit against an approved assistance case.</summary>
public sealed record RecordAssistanceDisbursementCommand(
    Guid CaseId,
    AssistanceDisbursementMethod DisbursementMethod,
    decimal Amount,
    string PayeeName,
    Guid? TargetInvoiceId = null,
    string? ReferenceNumber = null,
    string? Notes = null,
    Guid? ApprovedByPersonId = null
) : ICommand<AssistanceCaseResponse>;
