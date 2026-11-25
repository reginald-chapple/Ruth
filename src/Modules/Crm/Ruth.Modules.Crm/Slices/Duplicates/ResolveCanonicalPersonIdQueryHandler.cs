using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Duplicates;

internal sealed class ResolveCanonicalPersonIdQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<ResolveCanonicalPersonIdQuery, CanonicalPersonResolutionResponse>
{
    public async Task<Result<CanonicalPersonResolutionResponse>> HandleAsync(
        ResolveCanonicalPersonIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var current = PersonId.From(query.PersonOrExternalId);
        var visited = new HashSet<PersonId> { current };
        var hops = 0;

        for (var i = 0; i < 10; i++)
        {
            var next = await dbContext
                .PersonRedirects.AsNoTracking()
                .Where(r =>
                    r.SourcePersonId == current
                    || (
                        r.ExternalRecordGuid.HasValue && r.ExternalRecordGuid.Value == current.Value
                    )
                )
                .Select(static r => r.TargetPersonId)
                .FirstOrDefaultAsync(cancellationToken);

            if (next == PersonId.Empty || !visited.Add(next))
            {
                break;
            }

            current = next;
            hops++;
        }

        return Result.Success(
            new CanonicalPersonResolutionResponse(
                query.PersonOrExternalId,
                current.Value,
                hops > 0,
                hops
            )
        );
    }
}
