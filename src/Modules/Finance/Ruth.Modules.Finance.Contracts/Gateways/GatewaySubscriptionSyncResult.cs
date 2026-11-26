namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Result returned after synchronizing a recurring subscription schedule with a payment gateway.</summary>
/// <param name="GatewaySubscriptionReference">External gateway subscription identifier.</param>
/// <param name="SyncedAtUtc">UTC timestamp of synchronization.</param>
public sealed record GatewaySubscriptionSyncResult(
    string GatewaySubscriptionReference,
    DateTimeOffset SyncedAtUtc
);
