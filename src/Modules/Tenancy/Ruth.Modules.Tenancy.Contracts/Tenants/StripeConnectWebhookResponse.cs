namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Represents the outcome of processing an inbound Stripe Connect webhook delivery.</summary>
public sealed record StripeConnectWebhookResponse(
    string EventId,
    string EventType,
    string AccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted
);
