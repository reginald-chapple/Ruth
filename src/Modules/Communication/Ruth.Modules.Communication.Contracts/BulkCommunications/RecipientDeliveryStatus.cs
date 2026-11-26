namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Per-recipient delivery lifecycle state within a bulk communication.</summary>
public enum RecipientDeliveryStatus
{
    /// <summary>Queued and awaiting batch transport dispatch.</summary>
    Pending = 0,

    /// <summary>Accepted by the outbound communication transport.</summary>
    Delivered = 1,

    /// <summary>Failed during template rendering or transport submission.</summary>
    Failed = 2,

    /// <summary>Skipped because the recipient opted out of bulk or topic messages.</summary>
    SkippedUnsubscribed = 3,

    /// <summary>Reported as bounced or complained via provider webhook.</summary>
    Bounced = 4,
}
