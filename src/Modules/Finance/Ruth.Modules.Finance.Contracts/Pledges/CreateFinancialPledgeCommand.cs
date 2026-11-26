using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Pledges;

/// <summary>Command to record a new constituent or corporate financial pledge.</summary>
/// <param name="PersonId">Constituent or Business organization Person identifier.</param>
/// <param name="AccountId">Target financial account identifier.</param>
/// <param name="TotalAmount">Total pledged monetary amount (must be positive).</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="Frequency">Installment cadence.</param>
/// <param name="StartDate">Inclusive pledge start date.</param>
/// <param name="EndDate">Inclusive pledge end date.</param>
/// <param name="HouseholdId">Optional combined Household identifier.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="Notes">Optional pledge notes.</param>
public sealed record CreateFinancialPledgeCommand(
    Guid PersonId,
    Guid AccountId,
    decimal TotalAmount,
    string CurrencyCode,
    PledgeFrequency Frequency,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? HouseholdId = null,
    Guid? BranchId = null,
    string? Notes = null
) : ICommand<FinancialPledgeResponse>;
