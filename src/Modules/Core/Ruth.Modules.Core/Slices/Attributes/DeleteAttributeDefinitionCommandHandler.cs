using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Attributes;

internal sealed class DeleteAttributeDefinitionCommandHandler(
    CoreDbContext dbContext,
    ICoreReadCache cache
) : ICommandHandler<DeleteAttributeDefinitionCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteAttributeDefinitionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = AttributeDefinitionId.From(command.AttributeDefinitionId);
        var entity = await dbContext.AttributeDefinitions.FirstOrDefaultAsync(
            a => a.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return CoreErrors.AttributeNotFound;
        }

        if (entity.IsSystem)
        {
            return CoreErrors.SystemItemProtected;
        }

        _ = dbContext.AttributeDefinitions.Remove(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.AttributesBucket, cancellationToken);
        return true;
    }
}
