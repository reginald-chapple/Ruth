using Ruth.Modules.Finance.Contracts.Pledges;

namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Read projection for a recurring charitable contribution or membership subscription schedule.</summary>
/// <param name="Id">Unique recurring schedule identifier.</param>
/// <param name="PersonId">Constituent or Business organization Person identifier.</param>
/// <param name="HouseholdId">Optional Household identifier.</param>
/// <param name="FinancialGatewayId">Bound financial gateway identifier.</param>
/// <param name="SavedPaymentMethodId">Bound vaulted payment method identifier.</param>
/// <param name="MaskedPaymentMethod">Masked payment method summary.</param>
/// <param name="GatewaySubscriptionReference">Optional external gateway subscription ID.</param>
/// <param name="Purpose">Operational purpose (CharitableContribution, MembershipSubscription, ProgramRetainer).</param>
/// <param name="Frequency">Installment cadence.</param>
/// <param name="Status">Current lifecycle status.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="TotalAmount">Total installment amount.</param>
/// <param name="StartDate">Schedule start date.</param>
/// <param name="EndDate">Optional schedule end date.</param>
/// <param name="NextPaymentDate">Next scheduled billing date.</param>
/// <param name="LastProcessedAtUtc">Optional UTC timestamp of last settlement attempt.</param>
/// <param name="ConsecutiveFailureCount">Count of consecutive failed payment attempts.</param>
/// <param name="LastFailureReason">Optional failure message from most recent failed attempt.</param>
/// <param name="Splits">Account allocation splits.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record RecurringScheduleResponse(
    Guid Id,
    Guid PersonId,
    Guid? HouseholdId,
    Guid FinancialGatewayId,
    Guid SavedPaymentMethodId,
    string MaskedPaymentMethod,
    string? GatewaySubscriptionReference,
    RecurringSchedulePurpose Purpose,
    PledgeFrequency Frequency,
    RecurringScheduleStatus Status,
    string CurrencyCode,
    decimal TotalAmount,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly NextPaymentDate,
    DateTimeOffset? LastProcessedAtUtc,
    int ConsecutiveFailureCount,
    string? LastFailureReason,
    IReadOnlyList<RecurringScheduleSplitResponse> Splits,
    DateTimeOffset CreatedOnUtc
);
