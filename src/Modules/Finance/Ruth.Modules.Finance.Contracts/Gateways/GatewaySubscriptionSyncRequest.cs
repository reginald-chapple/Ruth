using Ruth.Modules.Finance.Contracts.Pledges;

namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Request payload to synchronize a recurring schedule with a payment gateway.</summary>
/// <param name="ScheduleId">Ruth recurring schedule identifier.</param>
/// <param name="CustomerVaultToken">Reusable gateway customer vault token.</param>
/// <param name="Amount">Scheduled installment amount.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="Frequency">Installment cadence.</param>
/// <param name="NextPaymentDate">Next scheduled billing date.</param>
/// <param name="Action">Lifecycle sync action.</param>
/// <param name="ExistingSubscriptionReference">Existing external gateway subscription ID if already created.</param>
/// <param name="IsTestMode">Whether the gateway is operating in sandbox/test mode.</param>
public sealed record GatewaySubscriptionSyncRequest(
    Guid ScheduleId,
    string CustomerVaultToken,
    decimal Amount,
    string CurrencyCode,
    PledgeFrequency Frequency,
    DateOnly NextPaymentDate,
    GatewaySubscriptionAction Action,
    string? ExistingSubscriptionReference = null,
    bool IsTestMode = false
);
