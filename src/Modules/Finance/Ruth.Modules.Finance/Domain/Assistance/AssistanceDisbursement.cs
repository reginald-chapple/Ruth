using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Assistance;

/// <summary>Disbursement payment or internal invoice fee-waiver credit recorded against an <see cref="AssistanceCase" />.</summary>
public sealed class AssistanceDisbursement : Entity<AssistanceDisbursementId>
{
    private AssistanceDisbursement() { }

    internal AssistanceDisbursement(
        AssistanceDisbursementId id,
        AssistanceCaseId caseId,
        AssistanceDisbursementMethod disbursementMethod,
        decimal amount,
        string currencyCode,
        string payeeName,
        InvoiceId? targetInvoiceId,
        string? referenceNumber,
        string? notes,
        Guid? approvedByPersonId,
        DateTimeOffset disbursedAtUtc
    )
        : base(id)
    {
        CaseId = caseId;
        DisbursementMethod = disbursementMethod;
        Amount = amount;
        CurrencyCode = currencyCode;
        PayeeName = payeeName;
        TargetInvoiceId = targetInvoiceId;
        ReferenceNumber = referenceNumber;
        Notes = notes;
        ApprovedByPersonId = approvedByPersonId;
        DisbursedAtUtc = disbursedAtUtc.ToUniversalTime();
    }

    public AssistanceCaseId CaseId { get; private set; }
    public AssistanceDisbursementMethod DisbursementMethod { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public string PayeeName { get; private set; } = string.Empty;
    public InvoiceId? TargetInvoiceId { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public Guid? ApprovedByPersonId { get; private set; }
    public DateTimeOffset DisbursedAtUtc { get; private set; }
}
