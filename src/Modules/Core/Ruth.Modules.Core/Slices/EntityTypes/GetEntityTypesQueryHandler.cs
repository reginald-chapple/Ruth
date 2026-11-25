using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.EntityTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.EntityTypes;

internal sealed class GetEntityTypesQueryHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : IQueryHandler<GetEntityTypesQuery, IReadOnlyList<EntityTypeResponse>>
{
    public async Task<Result<IReadOnlyList<EntityTypeResponse>>> HandleAsync(
        GetEntityTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var all = await cache.GetOrLoadAsync(
            CoreReadCache.EntityTypesBucket,
            async ct =>
                await dbContext
                    .EntityTypeDescriptors.AsNoTracking()
                    .OrderBy(static e => e.ModuleName)
                    .ThenBy(static e => e.FriendlyName)
                    .Select(static e => new EntityTypeResponse(
                        e.Id.Value,
                        e.SystemGuid,
                        e.Key,
                        e.Name,
                        e.FriendlyName,
                        e.ModuleName,
                        e.IsEntity,
                        e.IsSecured,
                        e.SupportsAttributes
                    ))
                    .ToListAsync(ct),
            cancellationToken
        );

        IEnumerable<EntityTypeResponse> filtered = all;
        if (query.OnlySecured == true)
        {
            filtered = filtered.Where(static e => e.IsSecured);
        }

        if (query.OnlySupportingAttributes == true)
        {
            filtered = filtered.Where(static e => e.SupportsAttributes);
        }

        return Result.Success<IReadOnlyList<EntityTypeResponse>>([.. filtered]);
    }
}
