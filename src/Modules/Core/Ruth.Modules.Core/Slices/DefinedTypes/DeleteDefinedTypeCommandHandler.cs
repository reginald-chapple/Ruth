using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal sealed class DeleteDefinedTypeCommandHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : ICommandHandler<DeleteDefinedTypeCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteDefinedTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = DefinedTypeId.From(command.DefinedTypeId);
        var entity = await dbContext.DefinedTypes.FirstOrDefaultAsync(
            dt => dt.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return CoreErrors.DefinedTypeNotFound;
        }

        if (entity.IsSystem)
        {
            return CoreErrors.SystemItemProtected;
        }

        _ = dbContext.DefinedTypes.Remove(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.DefinedTypesBucket, cancellationToken);
        return true;
    }
}
