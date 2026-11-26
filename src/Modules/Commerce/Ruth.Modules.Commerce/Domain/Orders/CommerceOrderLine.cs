using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain.Orders;

/// <summary>Persisted line item belonging to a <see cref="CommerceOrder" />.</summary>
public sealed class CommerceOrderLine : Entity<CommerceOrderLineId>
{
    private CommerceOrderLine() { }

    internal CommerceOrderLine(
        CommerceOrderLineId id,
        CommerceOrderId orderId,
        EvaluatedCommerceLinePlan plan
    )
        : base(id)
    {
        OrderId = orderId;
        ProductId = plan.Product.Id;
        VariantId = plan.Variant.Id;
        Sku = plan.Variant.Sku;
        ProductName = plan.Product.Name;
        VariantName = plan.Variant.Name;
        ProductKind = plan.Product.Kind;
        Quantity = plan.Quantity;
        ListUnitPrice = plan.ListUnitPrice;
        UnitMemberDiscountAmount = plan.UnitMemberDiscountAmount;
        NetUnitPrice = plan.NetUnitPrice;
        FairMarketValuePerUnit = plan.FairMarketValuePerUnit;
        LineSubtotalAmount = plan.LineSubtotalAmount;
        LineTaxAmount = plan.LineTaxAmount;
        LineShippingFeeAmount = plan.LineShippingFeeAmount;
        NonDeductibleRevenueAmount = plan.NonDeductibleRevenueAmount;
        RevenueAccountId = plan.RevenueAccountId;
        TaxDeductibleContributionAmount = plan.TaxDeductibleContributionAmount;
        CharitableAccountId = plan.CharitableAccountId;
        AppliedMemberPricingRuleId = plan.AppliedRuleId;
        AppliedMemberPricingRuleName = plan.AppliedRuleName;
        DigitalBinaryFileId = plan.Variant.DigitalBinaryFileId;
        PunchPassTypeId = plan.Variant.PunchPassTypeId;
    }

    public CommerceOrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public ProductVariantId VariantId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public string VariantName { get; private set; } = string.Empty;
    public ProductKind ProductKind { get; private set; }
    public int Quantity { get; private set; }
    public decimal ListUnitPrice { get; private set; }
    public decimal UnitMemberDiscountAmount { get; private set; }
    public decimal NetUnitPrice { get; private set; }
    public decimal FairMarketValuePerUnit { get; private set; }
    public decimal LineSubtotalAmount { get; private set; }
    public decimal LineTaxAmount { get; private set; }
    public decimal LineShippingFeeAmount { get; private set; }
    public decimal NonDeductibleRevenueAmount { get; private set; }
    public Guid RevenueAccountId { get; private set; }
    public decimal TaxDeductibleContributionAmount { get; private set; }
    public Guid? CharitableAccountId { get; private set; }
    public MemberPricingRuleId? AppliedMemberPricingRuleId { get; private set; }
    public string? AppliedMemberPricingRuleName { get; private set; }
    public Guid? DigitalBinaryFileId { get; private set; }
    public PunchPassTypeId? PunchPassTypeId { get; private set; }
    public IssuedPunchPassId? IssuedPunchPassId { get; private set; }

    internal void AttachIssuedPunchPass(IssuedPunchPassId issuedPunchPassId)
    {
        IssuedPunchPassId = issuedPunchPassId;
    }
}
