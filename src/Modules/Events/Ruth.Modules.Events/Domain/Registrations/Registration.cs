using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Registration aggregate coordinating registrar details, confirmed/waitlisted registrants, discounts, invoices, and installment payments.</summary>
public sealed class Registration
    : AggregateRoot<RegistrationId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Events.Registration";

    private readonly List<RegistrationRegistrant> _registrants = [];
    private readonly List<RegistrationPaymentRecord> _payments = [];

    private Registration() { }

    private Registration(RegistrationId id, string confirmationCode)
        : base(id)
    {
        ConfirmationCode = confirmationCode;
    }

    public RegistrationInstanceId InstanceId { get; private set; }
    public string ConfirmationCode { get; private set; } = string.Empty;
    public Guid? RegistrarPersonId { get; private set; }
    public string RegistrarFirstName { get; private set; } = string.Empty;
    public string RegistrarLastName { get; private set; } = string.Empty;
    public string RegistrarEmail { get; private set; } = string.Empty;
    public RegistrationStatus Status { get; private set; } = RegistrationStatus.Active;
    public string CurrencyCode { get; private set; } = "USD";
    public string? AppliedDiscountCode { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal DiscountTotalAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal BalanceDue => Math.Max(0m, TotalAmount - PaidAmount);
    public Guid? LinkedInvoiceId { get; private set; }

    public IReadOnlyCollection<RegistrationRegistrant> Registrants => _registrants.AsReadOnly();
    public IReadOnlyCollection<RegistrationPaymentRecord> Payments => _payments.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<Registration> Create(
        RegistrationInstanceId instanceId,
        Guid? registrarPersonId,
        string registrarFirstName,
        string registrarLastName,
        string registrarEmail,
        string currencyCode,
        EvaluatedRegistrationPlan plan,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (
            string.IsNullOrWhiteSpace(registrarFirstName)
            || string.IsNullOrWhiteSpace(registrarLastName)
            || string.IsNullOrWhiteSpace(registrarEmail)
            || !registrarEmail.Contains('@', StringComparison.Ordinal)
        )
        {
            return EventsRegistrationErrors.FormValidationFailed;
        }

        var id = RegistrationId.Create();
        var code = $"REG-{id.Value.ToString("N")[^8..].ToUpperInvariant()}";
        var registration = new Registration(id, code)
        {
            InstanceId = instanceId,
            RegistrarPersonId = registrarPersonId is { } rp && rp != Guid.Empty ? rp : null,
            RegistrarFirstName = registrarFirstName.Trim(),
            RegistrarLastName = registrarLastName.Trim(),
            RegistrarEmail = registrarEmail.Trim(),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            AppliedDiscountCode = plan.AppliedDiscountCode,
        };

        foreach (var regPlan in plan.Registrants)
        {
            registration._registrants.Add(
                new RegistrationRegistrant(
                    RegistrationRegistrantId.Create(),
                    id,
                    instanceId,
                    regPlan,
                    nowUtc
                )
            );
        }

        registration.RecalculateTotals();
        return registration;
    }

    public void SetLinkedInvoice(Guid invoiceId)
    {
        if (invoiceId != Guid.Empty)
        {
            LinkedInvoiceId = invoiceId;
        }
    }

    public Result<RegistrationPaymentRecord> RecordPayment(
        Guid financialTransactionId,
        decimal amount,
        DateTimeOffset paidAtUtc,
        string? note = null
    )
    {
        if (financialTransactionId == Guid.Empty || amount <= 0m || amount > BalanceDue)
        {
            return EventsRegistrationErrors.PaymentAmountInvalid;
        }

        var payment = new RegistrationPaymentRecord(
            RegistrationPaymentRecordId.Create(),
            Id,
            financialTransactionId,
            amount,
            paidAtUtc,
            note
        );
        _payments.Add(payment);
        RecalculateTotals();
        return payment;
    }

    public Result<bool> CancelRegistrant(RegistrationRegistrantId registrantId)
    {
        var registrant = _registrants.Find(r => r.Id == registrantId);
        if (registrant is null)
        {
            return EventsRegistrationErrors.RegistrantNotFound;
        }

        var wasConfirmed = registrant.Status == RegistrantStatus.Confirmed;
        _ = registrant.Cancel();
        if (_registrants.TrueForAll(static r => r.Status == RegistrantStatus.Cancelled))
        {
            Status = RegistrationStatus.Cancelled;
        }

        RecalculateTotals();
        return wasConfirmed;
    }

    public bool PromoteRegistrantFromWaitlist(
        RegistrationRegistrantId registrantId,
        decimal confirmedBaseCost
    )
    {
        var registrant = _registrants.Find(r => r.Id == registrantId);
        if (registrant is null || !registrant.PromoteFromWaitlist(confirmedBaseCost))
        {
            return false;
        }

        Status = RegistrationStatus.Active;
        RecalculateTotals();
        return true;
    }

    public void RecalculateTotals()
    {
        var confirmed = _registrants
            .Where(static r => r.Status == RegistrantStatus.Confirmed)
            .ToArray();
        SubtotalAmount = decimal.Round(
            confirmed.Sum(static r => r.BaseCost + r.FeesTotal),
            2,
            MidpointRounding.AwayFromZero
        );
        DiscountTotalAmount = decimal.Round(
            confirmed.Sum(static r => r.DiscountAmount),
            2,
            MidpointRounding.AwayFromZero
        );
        TotalAmount = decimal.Round(
            confirmed.Sum(static r => r.NetCost),
            2,
            MidpointRounding.AwayFromZero
        );
        PaidAmount = decimal.Round(
            _payments.Sum(static p => p.Amount),
            2,
            MidpointRounding.AwayFromZero
        );
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        var changed = false;
        if (RegistrarPersonId == retiredPersonId)
        {
            RegistrarPersonId = survivingPersonId;
            changed = true;
        }

        foreach (var registrant in _registrants)
        {
            changed |= registrant.RepointPerson(retiredPersonId, survivingPersonId);
        }

        return changed;
    }
}
