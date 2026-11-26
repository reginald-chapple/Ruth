namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Delivery and engagement telemetry event kinds accepted by the communication webhook API.</summary>
public enum CommunicationWebhookEventType
{
    /// <summary>Provider confirmed downstream delivery.</summary>
    Delivered = 0,

    /// <summary>Recipient opened the message.</summary>
    Opened = 1,

    /// <summary>Recipient clicked a tracked link in the message.</summary>
    Clicked = 2,

    /// <summary>Message bounced (soft or hard).</summary>
    Bounced = 3,

    /// <summary>Recipient reported spam complaint.</summary>
    SpamComplaint = 4,

    /// <summary>Recipient unsubscribed via provider header or link.</summary>
    Unsubscribed = 5,
}
