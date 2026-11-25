using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal sealed class ReorderDefinedValuesCommandHandler(
    CoreDbContext dbContext,
    ICoreReadCache cache
) : ICommandHandler<ReorderDefinedValuesCommand, IReadOnlyList<DefinedValueResponse>>
{
    public async Task<Result<IReadOnlyList<DefinedValueResponse>>> HandleAsync(
        ReorderDefinedValuesCommand command,
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

        definedType.ReorderValues(command.OrderedValueIds);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.DefinedTypesBucket, cancellationToken);

        IReadOnlyList<DefinedValueResponse> ordered =
        [
            .. definedType
                .Values.OrderBy(static v => v.Order)
                .Select(DefinedTypeMapper.ToValueResponse),
        ];
        return Result.Success(ordered);
    }
}
