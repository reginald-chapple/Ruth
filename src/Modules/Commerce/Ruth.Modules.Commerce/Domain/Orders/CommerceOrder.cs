using System.Globalization;
using System.Security.Cryptography;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Commerce.Domain.Orders;

/// <summary>Commerce order aggregate managing storefront checkout, IRS quid-pro-quo totals, fulfillment, and refunds.</summary>
public sealed class CommerceOrder
    : AggregateRoot<CommerceOrderId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Commerce.CommerceOrder";

    private readonly List<CommerceOrderLine> _lines = [];

    private CommerceOrder() { }

    private CommerceOrder(CommerceOrderId id)
        : base(id) { }

    public string OrderNumber { get; private set; } = string.Empty;
    public CommerceOrderStatus Status { get; private set; } = CommerceOrderStatus.PendingPayment;
    public Guid? BuyerPersonId { get; private set; }
    public string BuyerName { get; private set; } = string.Empty;
    public string BuyerEmail { get; private set; } = string.Empty;
    public Guid? BranchId { get; private set; }
    public CommerceFulfillmentMethod FulfillmentMethod { get; private set; }
    public string? ShippingAddressSummary { get; private set; }
    public string? TrackingReference { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal SubtotalAmount { get; private set; }
    public decimal MemberDiscountTotalAmount { get; private set; }
    public decimal TaxTotalAmount { get; private set; }
    public decimal ShippingFeeAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal NonDeductibleRevenueAmount { get; private set; }
    public decimal TaxDeductibleContributionAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public Guid? LinkedFinancialTransactionId { get; private set; }
    public Guid? LinkedInvoiceId { get; private set; }

    public IReadOnlyCollection<CommerceOrderLine> Lines => _lines.AsReadOnly();

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

    public static Result<CommerceOrder> Create(
        Guid? buyerPersonId,
        string buyerName,
        string buyerEmail,
        Guid? branchId,
        string? shippingAddressSummary,
        EvaluatedCommerceOrderPlan plan,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(plan);

        return !IsValidBuyerAndShipping(buyerName, buyerEmail, shippingAddressSummary, plan)
            ? (Result<CommerceOrder>)CommerceErrors.OrderCartInvalid
            : (Result<CommerceOrder>)
                BuildOrderInstance(
                    buyerPersonId,
                    buyerName,
                    buyerEmail,
                    branchId,
                    shippingAddressSummary,
                    plan,
                    nowUtc
                );
    }

    private static bool IsValidBuyerAndShipping(
        string? buyerName,
        string? buyerEmail,
        string? shippingAddress,
        EvaluatedCommerceOrderPlan plan
    )
    {
        var buyerOk =
            !string.IsNullOrWhiteSpace(buyerName)
            && !string.IsNullOrWhiteSpace(buyerEmail)
            && buyerEmail.Contains('@', StringComparison.Ordinal)
            && plan.Lines.Count > 0;
        var shippingOk =
            plan.FulfillmentMethod != CommerceFulfillmentMethod.FlatRateShipping
            || !string.IsNullOrWhiteSpace(shippingAddress);
        return buyerOk && shippingOk;
    }

    private static CommerceOrder BuildOrderInstance(
        Guid? buyerPersonId,
        string buyerName,
        string buyerEmail,
        Guid? branchId,
        string? shippingAddressSummary,
        EvaluatedCommerceOrderPlan plan,
        DateTimeOffset nowUtc
    )
    {
        var order = new CommerceOrder(CommerceOrderId.Create())
        {
            OrderNumber = GenerateOrderNumber(nowUtc),
            BuyerPersonId = NormalizeGuid(buyerPersonId),
            BuyerName = buyerName.Trim(),
            BuyerEmail = buyerEmail.Trim(),
            BranchId = NormalizeGuid(branchId),
            FulfillmentMethod = plan.FulfillmentMethod,
            ShippingAddressSummary = string.IsNullOrWhiteSpace(shippingAddressSummary)
                ? null
                : shippingAddressSummary.Trim(),
            CurrencyCode = plan.CurrencyCode,
            SubtotalAmount = plan.NetSubtotalAmount,
            MemberDiscountTotalAmount = plan.MemberDiscountTotalAmount,
            TaxTotalAmount = plan.TaxTotalAmount,
            ShippingFeeAmount = plan.ShippingFeeAmount,
            TotalAmount = plan.GrandTotalAmount,
            NonDeductibleRevenueAmount = plan.NonDeductibleRevenueAmount,
            TaxDeductibleContributionAmount = plan.TaxDeductibleContributionAmount,
            Status =
                plan.GrandTotalAmount == 0m
                    ? ResolvePostPaymentStatus(plan.Lines)
                    : CommerceOrderStatus.PendingPayment,
        };

        foreach (var linePlan in plan.Lines)
        {
            order._lines.Add(
                new CommerceOrderLine(CommerceOrderLineId.Create(), order.Id, linePlan)
            );
        }

        return order;
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }

    public void AttachInvoice(Guid invoiceId)
    {
        LinkedInvoiceId = invoiceId;
    }

    public void MarkPaid(Guid financialTransactionId, decimal paidAmount)
    {
        LinkedFinancialTransactionId = financialTransactionId;
        PaidAmount = decimal.Round(paidAmount, 2, MidpointRounding.AwayFromZero);
        var hasPhysical = _lines.Exists(static l =>
            l.ProductKind == ProductKind.PhysicalPickupOrFlatRate
        );
        Status = hasPhysical ? CommerceOrderStatus.Paid : CommerceOrderStatus.Fulfilled;
    }

    public Result AdvanceFulfillment(CommerceOrderStatus nextStatus, string? trackingReference)
    {
        if (!CanTransitionFulfillment(nextStatus))
        {
            return CommerceErrors.OrderStateTransitionInvalid;
        }

        Status = nextStatus;
        if (!string.IsNullOrWhiteSpace(trackingReference))
        {
            TrackingReference = trackingReference.Trim();
        }

        return Result.Success();
    }

    private bool CanTransitionFulfillment(CommerceOrderStatus nextStatus)
    {
        if (nextStatus == CommerceOrderStatus.Fulfilled)
        {
            return Status
                is CommerceOrderStatus.Paid
                    or CommerceOrderStatus.ReadyForPickup
                    or CommerceOrderStatus.Shipped;
        }

        var pickupAllowed =
            nextStatus == CommerceOrderStatus.ReadyForPickup
            && FulfillmentMethod == CommerceFulfillmentMethod.LocalPickup;
        var shippingAllowed =
            nextStatus == CommerceOrderStatus.Shipped
            && FulfillmentMethod == CommerceFulfillmentMethod.FlatRateShipping;

        return Status == CommerceOrderStatus.Paid && (pickupAllowed || shippingAllowed);
    }

    public Result RecordRefund(decimal refundAmount)
    {
        var rounded = decimal.Round(refundAmount, 2, MidpointRounding.AwayFromZero);
        var netSettled = PaidAmount - RefundedAmount;
        if (
            rounded <= 0m
            || rounded > netSettled
            || Status is CommerceOrderStatus.PendingPayment or CommerceOrderStatus.Cancelled
        )
        {
            return CommerceErrors.OrderStateTransitionInvalid;
        }

        RefundedAmount += rounded;
        Status =
            RefundedAmount >= PaidAmount
                ? CommerceOrderStatus.Refunded
                : CommerceOrderStatus.PartiallyRefunded;
        return Result.Success();
    }

    public bool IsEligibleForDigitalDownload()
    {
        return Status
            is CommerceOrderStatus.Paid
                or CommerceOrderStatus.ReadyForPickup
                or CommerceOrderStatus.Shipped
                or CommerceOrderStatus.Fulfilled
                or CommerceOrderStatus.PartiallyRefunded;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (BuyerPersonId != retiredPersonId)
        {
            return false;
        }

        BuyerPersonId = survivingPersonId;
        return true;
    }

    private static CommerceOrderStatus ResolvePostPaymentStatus(
        IReadOnlyList<EvaluatedCommerceLinePlan> lines
    )
    {
        var hasPhysical = lines.Any(static l =>
            l.Product.Kind == ProductKind.PhysicalPickupOrFlatRate
        );
        return hasPhysical ? CommerceOrderStatus.Paid : CommerceOrderStatus.Fulfilled;
    }

    private static string GenerateOrderNumber(DateTimeOffset nowUtc)
    {
        Span<byte> randomBytes = stackalloc byte[3];
        RandomNumberGenerator.Fill(randomBytes);
        var token = Convert.ToHexString(randomBytes);
        return string.Create(CultureInfo.InvariantCulture, $"ORD-{nowUtc:yyyyMMdd}-{token}");
    }
}
