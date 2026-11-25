using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Relationships;

internal sealed class GetPersonKnownRelationshipsQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetPersonKnownRelationshipsQuery, IReadOnlyList<KnownRelationshipResponse>>
{
    public async Task<Result<IReadOnlyList<KnownRelationshipResponse>>> HandleAsync(
        GetPersonKnownRelationshipsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var personId = PersonId.From(query.PersonId);
        var rows = await dbContext
            .KnownRelationships.AsNoTracking()
            .Where(r => r.OwnerPersonId == personId)
            .OrderBy(static r => r.RelationshipType)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Result.Success<IReadOnlyList<KnownRelationshipResponse>>([]);
        }

        var relatedIds = rows.Select(static r => r.RelatedPersonId).Distinct().ToArray();
        var people = await dbContext
            .People.AsNoTracking()
            .Where(p => relatedIds.Contains(p.Id))
            .ToDictionaryAsync(static p => p.Id, cancellationToken);

        IReadOnlyList<KnownRelationshipResponse> result =
        [
            .. rows.Select(r => new KnownRelationshipResponse(
                r.Id.Value,
                r.OwnerPersonId.Value,
                r.RelatedPersonId.Value,
                people.TryGetValue(r.RelatedPersonId, out var p)
                    ? p.FullName
                    : r.RelatedPersonId.ToString(),
                r.RelationshipType,
                r.InverseRelationshipId?.Value,
                r.Notes,
                r.CreatedOnUtc
            )),
        ];

        return Result.Success(result);
    }
}
