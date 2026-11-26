namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Lifecycle status of an individual participant check-in attendance entry.</summary>
public enum CheckInEntryStatus
{
    /// <summary>Participant is actively checked into the assigned room/location.</summary>
    CheckedIn = 0,

    /// <summary>Participant has been checked out of the room/location.</summary>
    CheckedOut = 1,

    /// <summary>Check-in entry was voided/cancelled.</summary>
    Cancelled = 2,
}
