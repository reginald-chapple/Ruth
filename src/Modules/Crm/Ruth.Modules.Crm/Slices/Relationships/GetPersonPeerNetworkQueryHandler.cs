using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Relationships;

internal sealed class GetPersonPeerNetworkQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetPersonPeerNetworkQuery, IReadOnlyList<PeerNetworkEdgeResponse>>
{
    public async Task<Result<IReadOnlyList<PeerNetworkEdgeResponse>>> HandleAsync(
        GetPersonPeerNetworkQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var personId = PersonId.From(query.PersonId);
        var edges = await dbContext
            .PeerNetworkEdges.AsNoTracking()
            .Where(e => e.SourcePersonId == personId && e.RelationshipScore > 0)
            .OrderByDescending(static e => e.RelationshipScore)
            .ToListAsync(cancellationToken);

        if (edges.Count == 0)
        {
            return Result.Success<IReadOnlyList<PeerNetworkEdgeResponse>>([]);
        }

        var targetIds = edges.Select(static e => e.TargetPersonId).Distinct().ToArray();
        var people = await dbContext
            .People.AsNoTracking()
            .Where(p => targetIds.Contains(p.Id))
            .ToDictionaryAsync(static p => p.Id, cancellationToken);

        IReadOnlyList<PeerNetworkEdgeResponse> result =
        [
            .. edges.Select(e => new PeerNetworkEdgeResponse(
                e.Id.Value,
                e.SourcePersonId.Value,
                e.TargetPersonId.Value,
                people.TryGetValue(e.TargetPersonId, out var p)
                    ? p.FullName
                    : e.TargetPersonId.ToString(),
                e.RelationshipType,
                e.RelatedEntityId,
                e.Strength,
                e.RelationshipScore,
                e.Trend,
                e.Caption,
                e.RelationshipStartDate,
                e.RelationshipEndDate,
                e.LastUpdatedOnUtc
            )),
        ];

        return Result.Success(result);
    }
}
