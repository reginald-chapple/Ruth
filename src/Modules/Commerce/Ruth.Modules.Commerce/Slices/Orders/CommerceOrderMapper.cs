using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Domain.Orders;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.Modules.Commerce.Domain.PunchPasses;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal static class CommerceOrderMapper
{
    public static CommerceOrderQuoteResponse MapQuote(EvaluatedCommerceOrderPlan plan)
    {
        return new(
            plan.CurrencyCode,
            plan.FulfillmentMethod,
            plan.ListSubtotalAmount,
            plan.MemberDiscountTotalAmount,
            plan.NetSubtotalAmount,
            plan.TaxTotalAmount,
            plan.ShippingFeeAmount,
            plan.GrandTotalAmount,
            plan.NonDeductibleRevenueAmount,
            plan.TaxDeductibleContributionAmount,
            plan.HasQuidProQuoDisclosure,
            [.. plan.Lines.Select(MapEvaluatedLine)],
            plan.AccountSplits
        );
    }

    public static CommerceOrderResponse MapOrder(CommerceOrder order)
    {
        return new(
            order.Id.Value,
            order.OrderNumber,
            order.Status,
            order.BuyerPersonId,
            order.BuyerName,
            order.BuyerEmail,
            order.BranchId,
            order.FulfillmentMethod,
            order.ShippingAddressSummary,
            order.TrackingReference,
            order.CurrencyCode,
            order.SubtotalAmount,
            order.MemberDiscountTotalAmount,
            order.TaxTotalAmount,
            order.ShippingFeeAmount,
            order.TotalAmount,
            order.NonDeductibleRevenueAmount,
            order.TaxDeductibleContributionAmount,
            order.PaidAmount,
            order.RefundedAmount,
            order.LinkedFinancialTransactionId,
            order.LinkedInvoiceId,
            [.. order.Lines.Select(MapOrderLine)],
            [
                .. order
                    .Lines.Where(static l => l.IssuedPunchPassId.HasValue)
                    .Select(static l => l.IssuedPunchPassId!.Value.Value),
            ],
            order.CreatedOnUtc
        );
    }

    public static IssuedPunchPassResponse MapIssuedPunchPass(IssuedPunchPass pass, string typeName)
    {
        return new(
            pass.Id.Value,
            pass.PassCode,
            pass.PunchPassTypeId.Value,
            typeName,
            pass.OwnerPersonId,
            pass.SourceOrderId?.Value,
            pass.InitialCredits,
            pass.RemainingCredits,
            pass.Status,
            pass.AllowHouseholdSharing,
            pass.IssuedAtUtc,
            pass.ExpiresAtUtc,
            [
                .. pass
                    .LedgerEntries.OrderBy(static e => e.OccurredAtUtc)
                    .Select(static e => new PunchPassLedgerEntryResponse(
                        e.Id.Value,
                        e.EntryKind,
                        e.DeltaCredits,
                        e.BalanceAfter,
                        e.RedeemedForPersonId,
                        e.ReferenceNotes,
                        e.OccurredAtUtc
                    )),
            ]
        );
    }

    private static CommerceOrderLineResponse MapEvaluatedLine(EvaluatedCommerceLinePlan line)
    {
        return new(
            Guid.Empty,
            line.Product.Id.Value,
            line.Variant.Id.Value,
            line.Variant.Sku,
            line.Product.Name,
            line.Variant.Name,
            line.Product.Kind,
            line.Quantity,
            line.ListUnitPrice,
            line.UnitMemberDiscountAmount,
            line.NetUnitPrice,
            line.FairMarketValuePerUnit,
            line.LineSubtotalAmount,
            line.LineTaxAmount,
            line.LineShippingFeeAmount,
            line.NonDeductibleRevenueAmount,
            line.RevenueAccountId,
            line.TaxDeductibleContributionAmount,
            line.CharitableAccountId,
            line.AppliedRuleId?.Value,
            line.AppliedRuleName,
            line.Variant.DigitalBinaryFileId,
            IssuedPunchPassId: null
        );
    }

    private static CommerceOrderLineResponse MapOrderLine(CommerceOrderLine line)
    {
        return new(
            line.Id.Value,
            line.ProductId.Value,
            line.VariantId.Value,
            line.Sku,
            line.ProductName,
            line.VariantName,
            line.ProductKind,
            line.Quantity,
            line.ListUnitPrice,
            line.UnitMemberDiscountAmount,
            line.NetUnitPrice,
            line.FairMarketValuePerUnit,
            line.LineSubtotalAmount,
            line.LineTaxAmount,
            line.LineShippingFeeAmount,
            line.NonDeductibleRevenueAmount,
            line.RevenueAccountId,
            line.TaxDeductibleContributionAmount,
            line.CharitableAccountId,
            line.AppliedMemberPricingRuleId?.Value,
            line.AppliedMemberPricingRuleName,
            line.DigitalBinaryFileId,
            line.IssuedPunchPassId?.Value
        );
    }
}
