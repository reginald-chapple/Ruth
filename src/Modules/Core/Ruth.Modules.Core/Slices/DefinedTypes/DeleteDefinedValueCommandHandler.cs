using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal sealed class DeleteDefinedValueCommandHandler(
    CoreDbContext dbContext,
    ICoreReadCache cache
) : ICommandHandler<DeleteDefinedValueCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteDefinedValueCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var typeId = DefinedTypeId.From(command.DefinedTypeId);
        var definedType = await dbContext
            .DefinedTypes.Include(static dt => dt.Values)
            .FirstOrDefaultAsync(dt => dt.Id == typeId, cancellationToken);
        if (definedType is null)
        {
            return CoreErrors.DefinedTypeNotFound;
        }

        var removed = definedType.RemoveValue(DefinedValueId.From(command.DefinedValueId));
        if (removed.IsFailure)
        {
            return Result.Failure<bool>(removed.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.DefinedTypesBucket, cancellationToken);
        return true;
    }
}
