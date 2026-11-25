using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.FieldTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Attributes;

internal sealed class SetGlobalAttributeValueCommandHandler(
    CoreDbContext dbContext,
    IFieldTypeRegistry fieldTypes,
    ICoreReadCache cache
) : ICommandHandler<SetGlobalAttributeValueCommand, AttributeDefinitionResponse>
{
    public async Task<Result<AttributeDefinitionResponse>> HandleAsync(
        SetGlobalAttributeValueCommand command,
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

        var result = entity.SetGlobalValue(fieldTypes, command.Value);
        if (result.IsFailure)
        {
            return Result.Failure<AttributeDefinitionResponse>(result.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.AttributesBucket, cancellationToken);
        return AttributeMapper.ToResponse(entity, entityTypeKey: null);
    }
}
