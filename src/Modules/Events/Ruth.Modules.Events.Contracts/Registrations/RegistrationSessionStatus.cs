namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Lifecycle state of a TTL seat-hold RegistrationSession.</summary>
public enum RegistrationSessionStatus
{
    /// <summary>Active hold reserving confirmed or waitlist seats until expiration.</summary>
    Active = 0,

    /// <summary>Consumed by a completed Registration submission.</summary>
    Completed = 1,

    /// <summary>Expired after exceeding the configured seat-hold TTL.</summary>
    Expired = 2,

    /// <summary>Explicitly released before completion.</summary>
    Released = 3,
}
