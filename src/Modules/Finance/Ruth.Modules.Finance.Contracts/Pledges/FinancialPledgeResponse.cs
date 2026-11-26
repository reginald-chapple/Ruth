namespace Ruth.Modules.Finance.Contracts.Pledges;

/// <summary>Read projection for a constituent or corporate financial pledge, including live fulfillment progress.</summary>
/// <param name="Id">Unique pledge identifier.</param>
/// <param name="PersonId">Constituent or Business organization Person identifier.</param>
/// <param name="HouseholdId">Optional combined billing/giving Household identifier.</param>
/// <param name="AccountId">Target financial account or fund identifier.</param>
/// <param name="AccountName">Display name of the target financial account.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="TotalAmount">Total committed amount across the pledge window.</param>
/// <param name="FulfilledAmount">Settled amount contributed toward this account within the pledge window.</param>
/// <param name="RemainingAmount">Unfulfilled commitment balance (floored at zero).</param>
/// <param name="ExpectedToDateAmount">Pro-rata expected amount as of the evaluation date.</param>
/// <param name="FulfillmentPercent">Percentage of total commitment fulfilled (0..100+).</param>
/// <param name="Frequency">Installment cadence.</param>
/// <param name="InstallmentCount">Calculated number of scheduled installments.</param>
/// <param name="StartDate">Inclusive pledge start date.</param>
/// <param name="EndDate">Inclusive pledge end date.</param>
/// <param name="Status">Current lifecycle status.</param>
/// <param name="Notes">Optional stewardship or commitment notes.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record FinancialPledgeResponse(
    Guid Id,
    Guid PersonId,
    Guid? HouseholdId,
    Guid AccountId,
    string AccountName,
    Guid? BranchId,
    string CurrencyCode,
    decimal TotalAmount,
    decimal FulfilledAmount,
    decimal RemainingAmount,
    decimal ExpectedToDateAmount,
    decimal FulfillmentPercent,
    PledgeFrequency Frequency,
    int InstallmentCount,
    DateOnly StartDate,
    DateOnly EndDate,
    FinancialPledgeStatus Status,
    string? Notes,
    DateTimeOffset CreatedOnUtc
);
