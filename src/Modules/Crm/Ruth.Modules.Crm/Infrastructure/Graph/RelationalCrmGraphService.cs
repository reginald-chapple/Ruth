using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Infrastructure.Graph;

internal sealed class RelationalCrmGraphService(CrmDbContext dbContext) : ICrmGraphService
{
    public Task EnsureGraphInitializedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task UpsertPersonVertexAsync(
        Guid personId,
        string fullName,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    public Task UpsertHouseholdVertexAsync(
        Guid householdId,
        string householdName,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    public Task UpsertHouseholdMembershipEdgeAsync(
        Guid personId,
        string personName,
        Guid householdId,
        string householdName,
        string role,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    public Task RemoveHouseholdMembershipEdgeAsync(
        Guid personId,
        Guid householdId,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    public Task UpsertKnownRelationshipEdgeAsync(
        Guid ownerPersonId,
        string ownerName,
        Guid relatedPersonId,
        string relatedName,
        string relationshipType,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    public Task RemoveKnownRelationshipEdgeAsync(
        Guid ownerPersonId,
        Guid relatedPersonId,
        string relationshipType,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    public Task UpsertPeerNetworkEdgeAsync(
        Guid sourcePersonId,
        string sourceName,
        Guid targetPersonId,
        string targetName,
        string edgeKey,
        string detail,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    public Task RemovePersonVertexAsync(
        Guid personId,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    public async Task<PersonRelationshipGraphResponse> TraverseAsync(
        Guid personId,
        int maxHops,
        CancellationToken cancellationToken = default
    )
    {
        var clampedHops = Math.Clamp(maxHops, 1, 3);
        if (personId == Guid.Empty)
        {
            return new PersonRelationshipGraphResponse(personId, clampedHops, [], []);
        }

        var rootId = PersonId.From(personId);
        var rootExists = await dbContext
            .People.AsNoTracking()
            .AnyAsync(p => p.Id == rootId && !p.IsMerged, cancellationToken);
        if (!rootExists)
        {
            return new PersonRelationshipGraphResponse(personId, clampedHops, [], []);
        }

        var personHops = new Dictionary<PersonId, int> { [rootId] = 0 };
        var householdHops = new Dictionary<HouseholdId, int>();
        var edges = new HashSet<(Guid Src, Guid Dst, string Type, string Detail)>();

        await ExpandHopsAsync(clampedHops, personHops, householdHops, edges, cancellationToken);
        var nodes = await MaterializeNodesAsync(personHops, householdHops, cancellationToken);
        var validNodeIds = nodes.Select(static n => n.EntityId).ToHashSet();

        List<RelationshipGraphEdgeResponse> filteredEdges =
        [
            .. edges
                .Where(e => validNodeIds.Contains(e.Src) && validNodeIds.Contains(e.Dst))
                .OrderBy(static e => e.Type, StringComparer.Ordinal)
                .ThenBy(static e => e.Src)
                .ThenBy(static e => e.Dst)
                .Select(static e => new RelationshipGraphEdgeResponse(
                    e.Src,
                    e.Dst,
                    e.Type,
                    e.Detail
                )),
        ];

        return new PersonRelationshipGraphResponse(personId, clampedHops, nodes, filteredEdges);
    }

    private async Task ExpandHopsAsync(
        int clampedHops,
        Dictionary<PersonId, int> personHops,
        Dictionary<HouseholdId, int> householdHops,
        HashSet<(Guid Src, Guid Dst, string Type, string Detail)> edges,
        CancellationToken cancellationToken
    )
    {
        HashSet<PersonId> personFrontier = [.. personHops.Keys];
        HashSet<HouseholdId> householdFrontier = [];

        for (var hop = 1; hop <= clampedHops; hop++)
        {
            if (personFrontier.Count == 0 && householdFrontier.Count == 0)
            {
                break;
            }

            var nextPeople = new HashSet<PersonId>();
            var nextHouseholds = new HashSet<HouseholdId>();

            await ExpandFromPeopleAsync(
                hop,
                personFrontier,
                personHops,
                householdHops,
                nextPeople,
                nextHouseholds,
                edges,
                cancellationToken
            );
            await ExpandFromHouseholdsAsync(
                hop,
                householdFrontier,
                personHops,
                nextPeople,
                edges,
                cancellationToken
            );

            personFrontier = nextPeople;
            householdFrontier = nextHouseholds;
        }
    }

    private async Task ExpandFromPeopleAsync(
        int hop,
        HashSet<PersonId> personFrontier,
        Dictionary<PersonId, int> personHops,
        Dictionary<HouseholdId, int> householdHops,
        HashSet<PersonId> nextPeople,
        HashSet<HouseholdId> nextHouseholds,
        HashSet<(Guid Src, Guid Dst, string Type, string Detail)> edges,
        CancellationToken cancellationToken
    )
    {
        if (personFrontier.Count == 0)
        {
            return;
        }

        PersonId[] frontierIds = [.. personFrontier];
        await ExpandPersonHouseholdsAsync(
            hop,
            frontierIds,
            householdHops,
            nextHouseholds,
            edges,
            cancellationToken
        );
        await ExpandKnownRelationshipsAsync(
            hop,
            frontierIds,
            personHops,
            nextPeople,
            edges,
            cancellationToken
        );
        await ExpandPeerNetworkEdgesAsync(
            hop,
            frontierIds,
            personHops,
            nextPeople,
            edges,
            cancellationToken
        );
    }

    private async Task ExpandPersonHouseholdsAsync(
        int hop,
        PersonId[] frontierIds,
        Dictionary<HouseholdId, int> householdHops,
        HashSet<HouseholdId> nextHouseholds,
        HashSet<(Guid Src, Guid Dst, string Type, string Detail)> edges,
        CancellationToken cancellationToken
    )
    {
        var memberships = await dbContext
            .HouseholdMembers.AsNoTracking()
            .Where(m => frontierIds.Contains(m.PersonId))
            .Select(static m => new
            {
                m.PersonId,
                m.HouseholdId,
                m.Role,
            })
            .ToListAsync(cancellationToken);

        foreach (var m in memberships)
        {
            _ = edges.Add((m.PersonId.Value, m.HouseholdId.Value, "MEMBER_OF", m.Role.ToString()));
            if (householdHops.TryAdd(m.HouseholdId, hop))
            {
                _ = nextHouseholds.Add(m.HouseholdId);
            }
        }
    }

    private async Task ExpandKnownRelationshipsAsync(
        int hop,
        PersonId[] frontierIds,
        Dictionary<PersonId, int> personHops,
        HashSet<PersonId> nextPeople,
        HashSet<(Guid Src, Guid Dst, string Type, string Detail)> edges,
        CancellationToken cancellationToken
    )
    {
        var known = await dbContext
            .KnownRelationships.AsNoTracking()
            .Where(r =>
                frontierIds.Contains(r.OwnerPersonId) || frontierIds.Contains(r.RelatedPersonId)
            )
            .Select(static r => new
            {
                r.OwnerPersonId,
                r.RelatedPersonId,
                r.RelationshipType,
            })
            .ToListAsync(cancellationToken);

        foreach (var r in known)
        {
            _ = edges.Add(
                (
                    r.OwnerPersonId.Value,
                    r.RelatedPersonId.Value,
                    "KNOWN_RELATIONSHIP",
                    r.RelationshipType.ToString()
                )
            );
            TryDiscoverPerson(r.OwnerPersonId, hop, personHops, nextPeople);
            TryDiscoverPerson(r.RelatedPersonId, hop, personHops, nextPeople);
        }
    }

    private async Task ExpandPeerNetworkEdgesAsync(
        int hop,
        PersonId[] frontierIds,
        Dictionary<PersonId, int> personHops,
        HashSet<PersonId> nextPeople,
        HashSet<(Guid Src, Guid Dst, string Type, string Detail)> edges,
        CancellationToken cancellationToken
    )
    {
        var peers = await dbContext
            .PeerNetworkEdges.AsNoTracking()
            .Where(p =>
                frontierIds.Contains(p.SourcePersonId) || frontierIds.Contains(p.TargetPersonId)
            )
            .Select(static p => new
            {
                p.SourcePersonId,
                p.TargetPersonId,
                p.Caption,
            })
            .ToListAsync(cancellationToken);

        foreach (var p in peers)
        {
            _ = edges.Add(
                (p.SourcePersonId.Value, p.TargetPersonId.Value, "PEER_NETWORK", p.Caption)
            );
            TryDiscoverPerson(p.SourcePersonId, hop, personHops, nextPeople);
            TryDiscoverPerson(p.TargetPersonId, hop, personHops, nextPeople);
        }
    }

    private async Task ExpandFromHouseholdsAsync(
        int hop,
        HashSet<HouseholdId> householdFrontier,
        Dictionary<PersonId, int> personHops,
        HashSet<PersonId> nextPeople,
        HashSet<(Guid Src, Guid Dst, string Type, string Detail)> edges,
        CancellationToken cancellationToken
    )
    {
        if (householdFrontier.Count == 0)
        {
            return;
        }

        HouseholdId[] hhIds = [.. householdFrontier];
        var members = await dbContext
            .HouseholdMembers.AsNoTracking()
            .Where(m => hhIds.Contains(m.HouseholdId))
            .Select(static m => new
            {
                m.PersonId,
                m.HouseholdId,
                m.Role,
            })
            .ToListAsync(cancellationToken);

        foreach (var m in members)
        {
            _ = edges.Add((m.PersonId.Value, m.HouseholdId.Value, "MEMBER_OF", m.Role.ToString()));
            TryDiscoverPerson(m.PersonId, hop, personHops, nextPeople);
        }
    }

    private static void TryDiscoverPerson(
        PersonId candidate,
        int hop,
        Dictionary<PersonId, int> personHops,
        HashSet<PersonId> nextPeople
    )
    {
        if (personHops.TryAdd(candidate, hop))
        {
            _ = nextPeople.Add(candidate);
        }
    }

    private async Task<List<RelationshipGraphNodeResponse>> MaterializeNodesAsync(
        Dictionary<PersonId, int> personHops,
        Dictionary<HouseholdId, int> householdHops,
        CancellationToken cancellationToken
    )
    {
        PersonId[] personIds = [.. personHops.Keys];
        var people = await dbContext
            .People.AsNoTracking()
            .Where(p => personIds.Contains(p.Id) && !p.IsMerged)
            .Select(static p => new { p.Id, p.FullName })
            .ToListAsync(cancellationToken);

        var nodes = new List<RelationshipGraphNodeResponse>(people.Count + householdHops.Count);
        foreach (var p in people)
        {
            nodes.Add(
                new RelationshipGraphNodeResponse(
                    p.Id.Value,
                    "Person",
                    p.FullName,
                    personHops[p.Id]
                )
            );
        }

        if (householdHops.Count > 0)
        {
            HouseholdId[] hhIds = [.. householdHops.Keys];
            var households = await dbContext
                .Households.AsNoTracking()
                .Where(h => hhIds.Contains(h.Id) && !h.IsArchived)
                .Select(static h => new { h.Id, h.Name })
                .ToListAsync(cancellationToken);

            foreach (var h in households)
            {
                nodes.Add(
                    new RelationshipGraphNodeResponse(
                        h.Id.Value,
                        "Household",
                        h.Name,
                        householdHops[h.Id]
                    )
                );
            }
        }

        return
        [
            .. nodes
                .OrderBy(static n => n.HopDistance)
                .ThenBy(static n => n.DisplayName, StringComparer.OrdinalIgnoreCase),
        ];
    }
}
