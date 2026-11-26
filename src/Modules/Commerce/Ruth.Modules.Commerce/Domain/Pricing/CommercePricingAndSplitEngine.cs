using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Domain.Pricing;

/// <summary>First-principles pricing, automatic member discount, and IRS quid-pro-quo split engine.</summary>
public static class CommercePricingAndSplitEngine
{
    public static Result<EvaluatedCommerceOrderPlan> Evaluate(
        IReadOnlyList<CommerceCartItemDto> requestedItems,
        IReadOnlyDictionary<Guid, (Product Product, ProductVariant Variant)> catalogLookup,
        IReadOnlyList<MemberPricingRule> activeRules,
        IReadOnlySet<string> buyerTiers,
        IReadOnlySet<Guid> buyerGroupIds,
        IReadOnlySet<Guid> buyerGroupTypeIds,
        CommerceFulfillmentMethod? requestedFulfillment,
        ICommerceTaxCalculator taxCalculator,
        decimal? taxRateOverridePercent,
        DateOnly evaluationDate
    )
    {
        ArgumentNullException.ThrowIfNull(catalogLookup);
        ArgumentNullException.ThrowIfNull(activeRules);
        ArgumentNullException.ThrowIfNull(taxCalculator);

        var consolidated = ConsolidateCartItems(requestedItems);
        if (consolidated.Count == 0)
        {
            return CommerceErrors.OrderCartInvalid;
        }

        var resolvedLinesRes = ResolveCatalogItems(consolidated, catalogLookup, out var currency);
        if (resolvedLinesRes.IsFailure)
        {
            return Result.Failure<EvaluatedCommerceOrderPlan>(resolvedLinesRes.Errors);
        }

        var fulfillmentRes = ResolveFulfillmentMethod(resolvedLinesRes.Value, requestedFulfillment);
        if (fulfillmentRes.IsFailure)
        {
            return Result.Failure<EvaluatedCommerceOrderPlan>(fulfillmentRes.Errors);
        }

        var fulfillment = fulfillmentRes.Value;
        var evaluatedLines = BuildEvaluatedLines(
            resolvedLinesRes.Value,
            activeRules,
            buyerTiers,
            buyerGroupIds,
            buyerGroupTypeIds,
            fulfillment,
            taxCalculator,
            taxRateOverridePercent,
            evaluationDate
        );

        return ComposeOrderPlan(currency, fulfillment, evaluatedLines);
    }

    private static List<CommerceCartItemDto> ConsolidateCartItems(
        IReadOnlyList<CommerceCartItemDto>? items
    )
    {
        if (items is null || items.Count == 0)
        {
            return [];
        }

        var totals = new Dictionary<Guid, int>();
        foreach (var item in items)
        {
            if (item.VariantId == Guid.Empty || item.Quantity <= 0 || item.Quantity > 10_000)
            {
                return [];
            }

            totals[item.VariantId] = totals.GetValueOrDefault(item.VariantId) + item.Quantity;
        }

        return [.. totals.Select(static kvp => new CommerceCartItemDto(kvp.Key, kvp.Value))];
    }

    private static Result<
        List<(Product Product, ProductVariant Variant, int Quantity)>
    > ResolveCatalogItems(
        List<CommerceCartItemDto> consolidated,
        IReadOnlyDictionary<Guid, (Product Product, ProductVariant Variant)> lookup,
        out string currencyCode
    )
    {
        currencyCode = "USD";
        var resolved = new List<(Product Product, ProductVariant Variant, int Quantity)>(
            consolidated.Count
        );
        string? firstCurrency = null;

        foreach (var req in consolidated)
        {
            if (!lookup.TryGetValue(req.VariantId, out var entry))
            {
                return CommerceErrors.ProductNotFound;
            }

            firstCurrency ??= entry.Product.CurrencyCode;
            var check = ValidateCatalogEntry(
                entry.Product,
                entry.Variant,
                req.Quantity,
                firstCurrency
            );
            if (check.IsFailure)
            {
                return Result.Failure<
                    List<(Product Product, ProductVariant Variant, int Quantity)>
                >(check.Errors);
            }

            resolved.Add((entry.Product, entry.Variant, req.Quantity));
        }

        currencyCode = firstCurrency ?? "USD";
        return resolved;
    }

    private static Result ValidateCatalogEntry(
        Product product,
        ProductVariant variant,
        int quantity,
        string expectedCurrency
    )
    {
        return !product.IsActive
            || !variant.IsActive
            || !string.Equals(expectedCurrency, product.CurrencyCode, StringComparison.Ordinal)
                ? (Result)CommerceErrors.OrderCartInvalid
            : variant.TrackInventory && variant.StockQuantity < quantity
                ? CommerceErrors.InsufficientStock
            : Result.Success();
    }

    private static Result<CommerceFulfillmentMethod> ResolveFulfillmentMethod(
        List<(Product Product, ProductVariant Variant, int Quantity)> items,
        CommerceFulfillmentMethod? requested
    )
    {
        var hasPhysical = items.Exists(static x =>
            x.Product.Kind == ProductKind.PhysicalPickupOrFlatRate
        );
        if (!hasPhysical)
        {
            return CommerceFulfillmentMethod.DigitalOrInstant;
        }

        var method = requested ?? CommerceFulfillmentMethod.LocalPickup;
        var supported = method switch
        {
            CommerceFulfillmentMethod.LocalPickup => items.TrueForAll(static x =>
                x.Product.Kind != ProductKind.PhysicalPickupOrFlatRate || x.Product.AllowLocalPickup
            ),
            CommerceFulfillmentMethod.FlatRateShipping => items.TrueForAll(static x =>
                x.Product.Kind != ProductKind.PhysicalPickupOrFlatRate
                || x.Product.AllowFlatRateShipping
            ),
            CommerceFulfillmentMethod.DigitalOrInstant => false,
            _ => false,
        };

        return supported ? method : CommerceErrors.OrderCartInvalid;
    }

    private static List<EvaluatedCommerceLinePlan> BuildEvaluatedLines(
        List<(Product Product, ProductVariant Variant, int Quantity)> items,
        IReadOnlyList<MemberPricingRule> activeRules,
        IReadOnlySet<string> buyerTiers,
        IReadOnlySet<Guid> buyerGroupIds,
        IReadOnlySet<Guid> buyerGroupTypeIds,
        CommerceFulfillmentMethod fulfillment,
        ICommerceTaxCalculator taxCalculator,
        decimal? taxRateOverride,
        DateOnly today
    )
    {
        var list = new List<EvaluatedCommerceLinePlan>(items.Count);
        foreach (var (product, variant, qty) in items)
        {
            list.Add(
                EvaluateSingleLine(
                    product,
                    variant,
                    qty,
                    activeRules,
                    buyerTiers,
                    buyerGroupIds,
                    buyerGroupTypeIds,
                    fulfillment,
                    taxCalculator,
                    taxRateOverride,
                    today
                )
            );
        }

        return list;
    }

    private static EvaluatedCommerceLinePlan EvaluateSingleLine(
        Product product,
        ProductVariant variant,
        int qty,
        IReadOnlyList<MemberPricingRule> activeRules,
        IReadOnlySet<string> buyerTiers,
        IReadOnlySet<Guid> buyerGroupIds,
        IReadOnlySet<Guid> buyerGroupTypeIds,
        CommerceFulfillmentMethod fulfillment,
        ICommerceTaxCalculator taxCalculator,
        decimal? taxRateOverride,
        DateOnly today
    )
    {
        var (bestRule, unitDiscount) = SelectBestMemberDiscount(
            product,
            variant.UnitPrice,
            activeRules,
            buyerTiers,
            buyerGroupIds,
            buyerGroupTypeIds,
            today
        );

        var netUnit = variant.UnitPrice - unitDiscount;
        var lineSubtotal = netUnit * qty;
        var fmvUnit = product.IsQuidProQuo ? variant.FairMarketValueAmount : 0m;
        var (commercialMerchAmount, charitableAmount) = ComputeQuidProQuoSplit(
            product.IsQuidProQuo,
            lineSubtotal,
            fmvUnit * qty
        );

        var lineTax = product.IsTaxable
            ? taxCalculator.CalculateTax(
                commercialMerchAmount,
                product.FlatTaxRatePercent,
                taxRateOverride
            )
            : 0m;

        var lineShipping =
            fulfillment == CommerceFulfillmentMethod.FlatRateShipping
            && product.Kind == ProductKind.PhysicalPickupOrFlatRate
                ? product.FlatShippingFeeAmount * qty
                : 0m;

        return new EvaluatedCommerceLinePlan(
            product,
            variant,
            qty,
            variant.UnitPrice,
            unitDiscount,
            netUnit,
            fmvUnit,
            lineSubtotal,
            lineTax,
            lineShipping,
            commercialMerchAmount + lineTax + lineShipping,
            product.RevenueAccountId,
            charitableAmount,
            product.IsQuidProQuo ? product.QuidProQuoCharitableAccountId : null,
            bestRule?.Id,
            bestRule?.Name
        );
    }

    private static (MemberPricingRule? Rule, decimal UnitDiscount) SelectBestMemberDiscount(
        Product product,
        decimal listUnitPrice,
        IReadOnlyList<MemberPricingRule> rules,
        IReadOnlySet<string> buyerTiers,
        IReadOnlySet<Guid> buyerGroupIds,
        IReadOnlySet<Guid> buyerGroupTypeIds,
        DateOnly today
    )
    {
        MemberPricingRule? bestRule = null;
        var bestDiscount = 0m;

        foreach (var rule in rules)
        {
            if (
                !rule.IsEligibleForLine(
                    product,
                    buyerTiers,
                    buyerGroupIds,
                    buyerGroupTypeIds,
                    today
                )
            )
            {
                continue;
            }

            var candidate = rule.CalculateUnitDiscount(listUnitPrice);
            if (IsBetterCandidate(candidate, rule, bestDiscount, bestRule))
            {
                bestDiscount = candidate;
                bestRule = rule;
            }
        }

        return (bestRule, bestDiscount);
    }

    private static bool IsBetterCandidate(
        decimal candidateDiscount,
        MemberPricingRule candidateRule,
        decimal currentBestDiscount,
        MemberPricingRule? currentBestRule
    )
    {
        return candidateDiscount > 0m
            && (
                candidateDiscount > currentBestDiscount
                || (
                    candidateDiscount == currentBestDiscount
                    && currentBestRule is not null
                    && candidateRule.Priority < currentBestRule.Priority
                )
            );
    }

    private static (
        decimal CommercialAmount,
        decimal CharitableExcessAmount
    ) ComputeQuidProQuoSplit(bool isQuidProQuo, decimal netLineSubtotal, decimal totalFmv)
    {
        return !isQuidProQuo
            ? (netLineSubtotal, 0m)
            : (Math.Min(netLineSubtotal, totalFmv), Math.Max(0m, netLineSubtotal - totalFmv));
    }

    private static EvaluatedCommerceOrderPlan ComposeOrderPlan(
        string currency,
        CommerceFulfillmentMethod fulfillment,
        List<EvaluatedCommerceLinePlan> lines
    )
    {
        var listSubtotal = lines.Sum(static l => l.ListUnitPrice * l.Quantity);
        var memberDiscount = lines.Sum(static l => l.UnitMemberDiscountAmount * l.Quantity);
        var netSubtotal = lines.Sum(static l => l.LineSubtotalAmount);
        var taxTotal = lines.Sum(static l => l.LineTaxAmount);
        var shippingTotal = lines.Sum(static l => l.LineShippingFeeAmount);
        var nonDeductible = lines.Sum(static l => l.NonDeductibleRevenueAmount);
        var charitable = lines.Sum(static l => l.TaxDeductibleContributionAmount);
        var hasQuidProQuo = lines.Exists(static l => l.Product.IsQuidProQuo);

        return new EvaluatedCommerceOrderPlan(
            currency,
            fulfillment,
            listSubtotal,
            memberDiscount,
            netSubtotal,
            taxTotal,
            shippingTotal,
            netSubtotal + taxTotal + shippingTotal,
            nonDeductible,
            charitable,
            hasQuidProQuo,
            lines,
            CommerceLedgerSplitAggregator.AggregateSplits(lines)
        );
    }
}
