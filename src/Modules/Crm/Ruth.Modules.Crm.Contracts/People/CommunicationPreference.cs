namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Preferred communication channel for a person.</summary>
public enum CommunicationPreference
{
    /// <summary>Defer to recipient default preference.</summary>
    RecipientPreference = 0,

    /// <summary>Prefer email.</summary>
    Email = 1,

    /// <summary>Prefer SMS text messaging.</summary>
    Sms = 2,

    /// <summary>Prefer mobile push notifications.</summary>
    PushNotification = 3,
}
