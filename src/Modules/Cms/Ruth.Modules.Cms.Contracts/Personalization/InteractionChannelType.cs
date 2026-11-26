namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Categorization of an InteractionChannel.</summary>
public enum InteractionChannelType
{
    /// <summary>External web site or member portal.</summary>
    Website = 0,

    /// <summary>Outbound email, SMS, or push communication.</summary>
    Communication = 1,

    /// <summary>Facility or event check-in kiosk.</summary>
    CheckIn = 2,

    /// <summary>Event registration or interactive session.</summary>
    Events = 3,

    /// <summary>Native mobile application shell.</summary>
    MobileApp = 4,

    /// <summary>Digital signage, Apple TV, or Roku display.</summary>
    DigitalSignage = 5,

    /// <summary>External integration or custom telemetry channel.</summary>
    External = 6,
}
