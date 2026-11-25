namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Communication preference for group scheduling and notifications.</summary>
public enum GroupCommunicationPreference
{
    /// <summary>Use the person's default CRM communication preference.</summary>
    RecipientPreference = 0,

    /// <summary>Prefer email notifications.</summary>
    Email = 1,

    /// <summary>Prefer SMS notifications.</summary>
    Sms = 2,

    /// <summary>Prefer push notifications.</summary>
    PushNotification = 3,
}
