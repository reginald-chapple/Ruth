namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Seat status of an individual RegistrationRegistrant.</summary>
public enum RegistrantStatus
{
    /// <summary>Confirmed attendee occupying an active seat.</summary>
    Confirmed = 0,

    /// <summary>Waitlisted attendee eligible for FIFO promotion.</summary>
    Waitlisted = 1,

    /// <summary>Cancelled registrant whose seat was released.</summary>
    Cancelled = 2,
}
