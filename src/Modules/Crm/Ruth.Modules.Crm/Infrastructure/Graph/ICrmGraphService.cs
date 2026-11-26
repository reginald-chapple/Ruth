using Ruth.Modules.Crm.Contracts.Relationships;

namespace Ruth.Modules.Crm.Infrastructure.Graph;

/// <summary>Resolves multi-hop CRM relationship graphs over relational people, households, known relationships, and peer-network edges.</summary>
public interface ICrmGraphService
{
    /// <summary>Ensures graph backing structures are initialized.</summary>
    Task EnsureGraphInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>Notifies graph backing service of a Person vertex upsert.</summary>
    Task UpsertPersonVertexAsync(
        Guid personId,
        string fullName,
        CancellationToken cancellationToken = default
    );

    /// <summary>Notifies graph backing service of a Household vertex upsert.</summary>
    Task UpsertHouseholdVertexAsync(
        Guid householdId,
        string householdName,
        CancellationToken cancellationToken = default
    );

    /// <summary>Upserts a MEMBER_OF edge from a Person to a Household.</summary>
    Task UpsertHouseholdMembershipEdgeAsync(
        Guid personId,
        string personName,
        Guid householdId,
        string householdName,
        string role,
        CancellationToken cancellationToken = default
    );

    /// <summary>Removes a MEMBER_OF edge between a Person and a Household.</summary>
    Task RemoveHouseholdMembershipEdgeAsync(
        Guid personId,
        Guid householdId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Upserts a KNOWN_RELATIONSHIP directed edge between two Person vertices.</summary>
    Task UpsertKnownRelationshipEdgeAsync(
        Guid ownerPersonId,
        string ownerName,
        Guid relatedPersonId,
        string relatedName,
        string relationshipType,
        CancellationToken cancellationToken = default
    );

    /// <summary>Removes a KNOWN_RELATIONSHIP edge between two Person vertices.</summary>
    Task RemoveKnownRelationshipEdgeAsync(
        Guid ownerPersonId,
        Guid relatedPersonId,
        string relationshipType,
        CancellationToken cancellationToken = default
    );

    /// <summary>Upserts a PEER_NETWORK directed edge between two Person vertices.</summary>
    Task UpsertPeerNetworkEdgeAsync(
        Guid sourcePersonId,
        string sourceName,
        Guid targetPersonId,
        string targetName,
        string edgeKey,
        string detail,
        CancellationToken cancellationToken = default
    );

    /// <summary>Removes a retired Person vertex and its incident edges after a merge.</summary>
    Task RemovePersonVertexAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Executes a multi-hop relational graph traversal centered on <paramref name="personId" />.</summary>
    Task<PersonRelationshipGraphResponse> TraverseAsync(
        Guid personId,
        int maxHops,
        CancellationToken cancellationToken = default
    );
}
