namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Organization engagement / affiliation status of a person.</summary>
public enum PersonConnectionStatus
{
    /// <summary>First-time or returning guest/visitor.</summary>
    Visitor = 0,

    /// <summary>Regular attendee or active community participant.</summary>
    Attendee = 1,

    /// <summary>Enrolled or active member.</summary>
    Member = 2,

    /// <summary>Program or volunteer participant.</summary>
    Participant = 3,

    /// <summary>Prospective member or lead.</summary>
    Prospect = 4,
}
