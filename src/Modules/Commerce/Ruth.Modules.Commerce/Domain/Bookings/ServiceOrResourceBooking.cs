using System.Globalization;
using System.Security.Cryptography;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Commerce.Domain.Bookings;

/// <summary>Confirmed or historical 1-on-1 appointment, workshop reservation, or facility/resource rental.</summary>
public sealed class ServiceOrResourceBooking
    : AggregateRoot<ServiceBookingId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Commerce.ServiceOrResourceBooking";

    private ServiceOrResourceBooking() { }

    private ServiceOrResourceBooking(ServiceBookingId id)
        : base(id) { }

    public string ConfirmationCode { get; private set; } = string.Empty;
    public ServiceBookingStatus Status { get; private set; } = ServiceBookingStatus.Confirmed;
    public BookingTargetKind TargetKind { get; private set; }
    public BookableServiceId? BookableServiceId { get; private set; }
    public RentableResourceId? RentableResourceId { get; private set; }
    public string TargetName { get; private set; } = string.Empty;
    public BookingDeliveryMode DeliveryMode { get; private set; }
    public Guid? ProviderPersonId { get; private set; }
    public Guid? LocationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CustomerPersonId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public string? CustomerPhone { get; private set; }
    public string? CustomerNotes { get; private set; }
    public int ParticipantOrUnitCount { get; private set; }
    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }
    public DateTimeOffset EffectiveBlockStartUtc { get; private set; }
    public DateTimeOffset EffectiveBlockEndUtc { get; private set; }
    public BookingPricingKind PricingKind { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal ListAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public Guid? LinkedFinancialTransactionId { get; private set; }
    public Guid? LinkedInvoiceId { get; private set; }
    public IssuedPunchPassId? RedeemedPunchPassId { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }

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

    public static Result<ServiceOrResourceBooking> Create(
        BookingTargetKind targetKind,
        BookableServiceId? serviceId,
        RentableResourceId? resourceId,
        string targetName,
        BookingDeliveryMode deliveryMode,
        Guid? branchId,
        AvailableBookingSlotDto slot,
        int quantity,
        Guid? customerPersonId,
        string customerName,
        string customerEmail,
        string? customerPhone,
        string? customerNotes,
        BookingQuoteResponse quote,
        DateTimeOffset nowUtc
    )
    {
        return !IsValidCustomerPayload(customerName, customerEmail, quantity)
            ? (Result<ServiceOrResourceBooking>)CommerceErrors.BookingPaymentInvalid
            : (Result<ServiceOrResourceBooking>)
                BuildBookingInstance(
                    targetKind,
                    serviceId,
                    resourceId,
                    targetName,
                    deliveryMode,
                    branchId,
                    slot,
                    quantity,
                    customerPersonId,
                    customerName,
                    customerEmail,
                    customerPhone,
                    customerNotes,
                    quote,
                    nowUtc
                );
    }

    private static bool IsValidCustomerPayload(string? name, string? email, int quantity)
    {
        return !string.IsNullOrWhiteSpace(name)
            && !string.IsNullOrWhiteSpace(email)
            && email.Contains('@', StringComparison.Ordinal)
            && quantity > 0;
    }

    private static ServiceOrResourceBooking BuildBookingInstance(
        BookingTargetKind targetKind,
        BookableServiceId? serviceId,
        RentableResourceId? resourceId,
        string targetName,
        BookingDeliveryMode deliveryMode,
        Guid? branchId,
        AvailableBookingSlotDto slot,
        int quantity,
        Guid? customerPersonId,
        string customerName,
        string customerEmail,
        string? customerPhone,
        string? customerNotes,
        BookingQuoteResponse quote,
        DateTimeOffset nowUtc
    )
    {
        var code = GenerateConfirmationCode(nowUtc);

        return new ServiceOrResourceBooking(ServiceBookingId.Create())
        {
            ConfirmationCode = code,
            Status = ServiceBookingStatus.Confirmed,
            TargetKind = targetKind,
            BookableServiceId = serviceId,
            RentableResourceId = resourceId,
            TargetName = targetName,
            DeliveryMode = deliveryMode,
            ProviderPersonId = slot.ProviderPersonId,
            LocationId = slot.LocationId,
            BranchId = branchId,
            CustomerPersonId = NormalizeGuid(customerPersonId),
            CustomerName = customerName.Trim(),
            CustomerEmail = customerEmail.Trim(),
            CustomerPhone = TrimOptional(customerPhone),
            CustomerNotes = TrimOptional(customerNotes),
            ParticipantOrUnitCount = quantity,
            StartUtc = slot.SlotStartUtc.ToUniversalTime(),
            EndUtc = slot.SlotEndUtc.ToUniversalTime(),
            EffectiveBlockStartUtc = slot.EffectiveBlockStartUtc.ToUniversalTime(),
            EffectiveBlockEndUtc = slot.EffectiveBlockEndUtc.ToUniversalTime(),
            PricingKind = quote.PricingKind,
            CurrencyCode = quote.CurrencyCode,
            ListAmount = quote.ListAmount,
            DiscountAmount = quote.DiscountAmount,
            TotalAmount = quote.TotalAmount,
        };
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }

    private static string? TrimOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public void AttachPayment(Guid transactionId, decimal paidAmount)
    {
        LinkedFinancialTransactionId = transactionId;
        PaidAmount = decimal.Round(paidAmount, 2, MidpointRounding.AwayFromZero);
    }

    public void AttachInvoice(Guid invoiceId)
    {
        LinkedInvoiceId = invoiceId;
    }

    public void AttachPunchPass(IssuedPunchPassId passId)
    {
        RedeemedPunchPassId = passId;
    }

    public Result Cancel(
        int cancellationNoticeMinutes,
        bool staffOverride,
        string reason,
        DateTimeOffset nowUtc
    )
    {
        if (Status == ServiceBookingStatus.Cancelled || string.IsNullOrWhiteSpace(reason))
        {
            return CommerceErrors.BookingCancellationDenied;
        }

        if (!staffOverride && nowUtc > StartUtc.AddMinutes(-cancellationNoticeMinutes))
        {
            return CommerceErrors.BookingCancellationDenied;
        }

        Status = ServiceBookingStatus.Cancelled;
        CancelledAtUtc = nowUtc.ToUniversalTime();
        CancellationReason = reason.Trim();
        return Result.Success();
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        var changed = false;
        if (CustomerPersonId == retiredPersonId)
        {
            CustomerPersonId = survivingPersonId;
            changed = true;
        }

        if (ProviderPersonId == retiredPersonId)
        {
            ProviderPersonId = survivingPersonId;
            changed = true;
        }

        return changed;
    }

    private static string GenerateConfirmationCode(DateTimeOffset nowUtc)
    {
        Span<byte> randomBytes = stackalloc byte[3];
        RandomNumberGenerator.Fill(randomBytes);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"BKG-{nowUtc:yyyyMMdd}-{Convert.ToHexString(randomBytes)}"
        );
    }
}
