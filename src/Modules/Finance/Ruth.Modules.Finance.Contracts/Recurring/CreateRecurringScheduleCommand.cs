using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Command to create a recurring charitable gift or membership subscription schedule and sync with the gateway.</summary>
/// <param name="PersonId">Constituent or Business organization Person identifier.</param>
/// <param name="FinancialGatewayId">Target financial gateway configuration identifier.</param>
/// <param name="SavedPaymentMethodId">Target vaulted saved payment method identifier.</param>
/// <param name="Purpose">Operational purpose of the recurring schedule.</param>
/// <param name="Frequency">Installment cadence (cannot be OneTime).</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="StartDate">First scheduled payment date.</param>
/// <param name="Splits">One or more positive account splits.</param>
/// <param name="EndDate">Optional final payment date.</param>
/// <param name="HouseholdId">Optional Household identifier.</param>
public sealed record CreateRecurringScheduleCommand(
    Guid PersonId,
    Guid FinancialGatewayId,
    Guid SavedPaymentMethodId,
    RecurringSchedulePurpose Purpose,
    PledgeFrequency Frequency,
    string CurrencyCode,
    DateOnly StartDate,
    IReadOnlyList<RecurringScheduleSplitSpec> Splits,
    DateOnly? EndDate = null,
    Guid? HouseholdId = null
) : ICommand<RecurringScheduleResponse>;
