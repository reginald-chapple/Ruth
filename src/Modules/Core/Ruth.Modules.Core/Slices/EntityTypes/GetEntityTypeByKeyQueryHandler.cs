using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.EntityTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.EntityTypes;

internal sealed class GetEntityTypeByKeyQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetEntityTypeByKeyQuery, EntityTypeResponse>
{
    public async Task<Result<EntityTypeResponse>> HandleAsync(
        GetEntityTypeByKeyQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var key = query.Key?.Trim() ?? string.Empty;
        var entity = await dbContext
            .EntityTypeDescriptors.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        return entity is null
            ? CoreErrors.EntityTypeNotFound
            : new EntityTypeResponse(
                entity.Id.Value,
                entity.SystemGuid,
                entity.Key,
                entity.Name,
                entity.FriendlyName,
                entity.ModuleName,
                entity.IsEntity,
                entity.IsSecured,
                entity.SupportsAttributes
            );
    }
}
