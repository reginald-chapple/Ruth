namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>RSVP response state for a scheduled serving assignment or occurrence.</summary>
public enum RsvpStatus
{
    /// <summary>Volunteer has not yet responded.</summary>
    Unknown = 0,

    /// <summary>Volunteer declined the request.</summary>
    No = 1,

    /// <summary>Volunteer accepted/confirmed the request.</summary>
    Yes = 2,

    /// <summary>Volunteer responded tentatively.</summary>
    Maybe = 3,
}
