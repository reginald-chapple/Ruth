using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Finance.Contracts.Events;

/// <summary>Outbox integration event emitted when an assistance, scholarship, fee-waiver, or grant disbursement is recorded.</summary>
public sealed record AssistanceDisbursementRecordedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid CaseId,
    string CaseNumber,
    Guid ProgramId,
    Guid ApplicantPersonId,
    AssistanceDisbursementMethod DisbursementMethod,
    decimal Amount,
    string CurrencyCode,
    Guid? TargetInvoiceId
) : IIntegrationEvent;
