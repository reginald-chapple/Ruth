namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Delivery medium channel supported by modular communication transports.</summary>
public enum CommunicationChannel
{
    /// <summary>Electronic mail (SMTP, SendGrid, Mailgun, Dev Sink).</summary>
    Email = 0,

    /// <summary>Short Message Service text messaging (Twilio SMS, Dev Sink).</summary>
    Sms = 1,

    /// <summary>Mobile or web push notification (APNs/FCM/WebPush provider or stub).</summary>
    Push = 2,
}
