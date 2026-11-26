using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Payment record linking a settled Finance FinancialTransaction to a Registration.</summary>
public sealed class RegistrationPaymentRecord : Entity<RegistrationPaymentRecordId>
{
    private RegistrationPaymentRecord() { }

    internal RegistrationPaymentRecord(
        RegistrationPaymentRecordId id,
        RegistrationId registrationId,
        Guid financialTransactionId,
        decimal amount,
        DateTimeOffset paidAtUtc,
        string? note
    )
        : base(id)
    {
        RegistrationId = registrationId;
        FinancialTransactionId = financialTransactionId;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        PaidAtUtc = paidAtUtc.ToUniversalTime();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public RegistrationId RegistrationId { get; private set; }
    public Guid FinancialTransactionId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset PaidAtUtc { get; private set; }
    public string? Note { get; private set; }
}
