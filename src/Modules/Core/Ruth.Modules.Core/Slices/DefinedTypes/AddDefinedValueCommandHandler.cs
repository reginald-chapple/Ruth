using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal sealed class AddDefinedValueCommandHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : ICommandHandler<AddDefinedValueCommand, DefinedValueResponse>
{
    public async Task<Result<DefinedValueResponse>> HandleAsync(
        AddDefinedValueCommand command,
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

        var added = definedType.AddValue(
            command.Value,
            command.Description,
            command.CategoryId.HasValue ? CategoryId.From(command.CategoryId.Value) : null,
            command.Order
        );
        if (added.IsFailure)
        {
            return Result.Failure<DefinedValueResponse>(added.Errors);
        }

        if (command.Attributes is not null)
        {
            added.Value.ReplaceAttributes(command.Attributes);
        }

        _ = dbContext.DefinedValues.Add(added.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.DefinedTypesBucket, cancellationToken);
        return DefinedTypeMapper.ToValueResponse(added.Value);
    }
}
