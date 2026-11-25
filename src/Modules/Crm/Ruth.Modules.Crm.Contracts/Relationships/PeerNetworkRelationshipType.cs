namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Classification of a directed peer-network connection.</summary>
public enum PeerNetworkRelationshipType
{
    /// <summary>Explicit person-to-person following edge (base score 5).</summary>
    Following = 0,

    /// <summary>Connection derived from shared ministry group attendance/membership.</summary>
    GroupConnection = 1,

    /// <summary>Connection derived from a known relationship.</summary>
    KnownRelationship = 2,

    /// <summary>Connection derived from shared household membership.</summary>
    Household = 3,
}
