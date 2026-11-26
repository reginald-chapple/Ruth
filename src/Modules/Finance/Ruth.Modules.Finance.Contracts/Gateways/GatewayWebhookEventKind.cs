namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Normalized webhook event classification emitted by payment gateways.</summary>
public enum GatewayWebhookEventKind
{
    /// <summary>A recurring subscription installment settled successfully.</summary>
    SubscriptionChargeSucceeded = 0,

    /// <summary>A recurring subscription installment failed or was declined.</summary>
    SubscriptionChargeFailed = 1,

    /// <summary>A recurring subscription was cancelled externally at the gateway.</summary>
    SubscriptionCancelled = 2,

    /// <summary>A previously settled charge was refunded or reversed externally.</summary>
    ChargeRefunded = 3,
}
