namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Lifecycle operation synchronized to a payment gateway's recurring billing engine.</summary>
public enum GatewaySubscriptionAction
{
    /// <summary>Create or update the recurring subscription schedule.</summary>
    CreateOrUpdate = 0,

    /// <summary>Pause future recurring billings.</summary>
    Pause = 1,

    /// <summary>Resume a paused recurring subscription.</summary>
    Resume = 2,

    /// <summary>Permanently cancel the recurring subscription.</summary>
    Cancel = 3,
}
