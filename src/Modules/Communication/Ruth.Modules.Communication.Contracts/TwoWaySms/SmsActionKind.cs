namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Automated operations executed when an inbound SMS matches an SmsAction rule.</summary>
public enum SmsActionKind
{
    /// <summary>Renders a merge-enabled reply template and sends an automated SMS back.</summary>
    AutoReply = 0,

    /// <summary>Records global SMS opt-out for the sender and sends a confirmation reply.</summary>
    OptOutSms = 1,

    /// <summary>Clears global SMS opt-out for the sender and sends a confirmation reply.</summary>
    OptInSms = 2,

    /// <summary>Triggers a named SystemCommunication notification.</summary>
    PublishSystemNotification = 3,
}
