using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Domain.Pledges;

namespace Ruth.Modules.Finance.Slices.Pledges;

internal static class FinancialPledgeMapper
{
    public static FinancialPledgeResponse ToResponse(
        FinancialPledge pledge,
        string accountName,
        decimal fulfilledAmount,
        DateOnly asOfDate
    )
    {
        var remaining = Math.Max(0m, pledge.TotalAmount - fulfilledAmount);
        var expectedToDate = pledge.CalculateExpectedByDate(asOfDate);
        var percent =
            pledge.TotalAmount > 0m
                ? decimal.Round(
                    fulfilledAmount / pledge.TotalAmount * 100m,
                    2,
                    MidpointRounding.AwayFromZero
                )
                : 0m;

        return new FinancialPledgeResponse(
            pledge.Id.Value,
            pledge.PersonId,
            pledge.HouseholdId,
            pledge.AccountId.Value,
            accountName,
            pledge.BranchId,
            pledge.CurrencyCode,
            pledge.TotalAmount,
            fulfilledAmount,
            remaining,
            expectedToDate,
            percent,
            pledge.Frequency,
            pledge.CalculateInstallmentCount(),
            pledge.StartDate,
            pledge.EndDate,
            pledge.Status,
            pledge.Notes,
            pledge.CreatedOnUtc
        );
    }
}
