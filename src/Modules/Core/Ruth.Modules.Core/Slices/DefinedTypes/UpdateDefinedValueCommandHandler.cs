using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal sealed class UpdateDefinedValueCommandHandler(
    CoreDbContext dbContext,
    ICoreReadCache cache
) : ICommandHandler<UpdateDefinedValueCommand, DefinedValueResponse>
{
    public async Task<Result<DefinedValueResponse>> HandleAsync(
        UpdateDefinedValueCommand command,
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

        var updated = definedType.UpdateValue(
            DefinedValueId.From(command.DefinedValueId),
            command.Value,
            command.Description,
            command.CategoryId.HasValue ? CategoryId.From(command.CategoryId.Value) : null,
            command.Order,
            command.IsActive
        );
        if (updated.IsFailure)
        {
            return Result.Failure<DefinedValueResponse>(updated.Errors);
        }

        if (command.Attributes is not null)
        {
            updated.Value.ReplaceAttributes(command.Attributes);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.DefinedTypesBucket, cancellationToken);
        return DefinedTypeMapper.ToValueResponse(updated.Value);
    }
}
