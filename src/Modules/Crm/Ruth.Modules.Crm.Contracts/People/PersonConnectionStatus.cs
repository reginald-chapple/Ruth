namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Church engagement / connection status of a person.</summary>
public enum PersonConnectionStatus
{
    /// <summary>First-time or returning visitor.</summary>
    Visitor = 0,

    /// <summary>Regular attendee.</summary>
    Attendee = 1,

    /// <summary>Formal member.</summary>
    Member = 2,

    /// <summary>Ministry or program participant.</summary>
    Participant = 3,

    /// <summary>Prospective connection.</summary>
    Prospect = 4,
}
