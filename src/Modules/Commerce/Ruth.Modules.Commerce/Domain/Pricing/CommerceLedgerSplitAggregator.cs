using Ruth.Modules.Commerce.Contracts.Orders;

namespace Ruth.Modules.Commerce.Domain.Pricing;

internal static class CommerceLedgerSplitAggregator
{
    public static List<CommerceLedgerSplitPreviewDto> AggregateSplits(
        IReadOnlyList<EvaluatedCommerceLinePlan> lines
    )
    {
        var grouped = new Dictionary<(Guid AccountId, bool IsCharitable), decimal>();

        foreach (var line in lines)
        {
            if (line.NonDeductibleRevenueAmount > 0m)
            {
                var key = (line.RevenueAccountId, false);
                grouped[key] = grouped.GetValueOrDefault(key) + line.NonDeductibleRevenueAmount;
            }

            if (line.TaxDeductibleContributionAmount > 0m && line.CharitableAccountId is { } cId)
            {
                var key = (cId, true);
                grouped[key] =
                    grouped.GetValueOrDefault(key) + line.TaxDeductibleContributionAmount;
            }
        }

        return
        [
            .. grouped.Select(static kvp => new CommerceLedgerSplitPreviewDto(
                kvp.Key.AccountId,
                kvp.Key.IsCharitable,
                kvp.Value,
                kvp.Key.IsCharitable
                    ? "IRS Quid-Pro-Quo Tax-Deductible Excess Contribution"
                    : "Commerce Operating Revenue, Goods FMV, Tax & Fulfillment"
            )),
        ];
    }
}
