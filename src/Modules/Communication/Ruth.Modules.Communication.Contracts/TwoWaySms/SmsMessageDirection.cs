namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Direction of a message inside a two-way SMS conversation thread.</summary>
public enum SmsMessageDirection
{
    /// <summary>Inbound SMS received from a constituent/member phone number.</summary>
    Inbound = 0,

    /// <summary>Outbound 1-to-1 reply sent manually by a staff member.</summary>
    OutboundReply = 1,

    /// <summary>Automated reply sent by an SmsPipeline keyword action.</summary>
    AutomatedReply = 2,
}
