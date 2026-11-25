using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.FieldTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Attributes;

internal sealed class UpdateAttributeDefinitionCommandHandler(
    CoreDbContext dbContext,
    IFieldTypeRegistry fieldTypes,
    ICoreReadCache cache
) : ICommandHandler<UpdateAttributeDefinitionCommand, AttributeDefinitionResponse>
{
    public async Task<Result<AttributeDefinitionResponse>> HandleAsync(
        UpdateAttributeDefinitionCommand command,
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

        var updated = entity.Update(
            fieldTypes,
            command.Name,
            command.Description,
            command.FieldTypeKey,
            command.IsRequired,
            command.IsGridColumn,
            command.IsMultiValue,
            command.IsActive,
            command.DefaultValue,
            command.Order,
            command.CategoryId.HasValue ? CategoryId.From(command.CategoryId.Value) : null,
            command.Qualifiers
        );
        if (updated.IsFailure)
        {
            return Result.Failure<AttributeDefinitionResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.AttributesBucket, cancellationToken);

        string? entityTypeKey = null;
        if (entity.EntityTypeId.HasValue)
        {
            var typeId = entity.EntityTypeId.Value;
            entityTypeKey = await dbContext
                .EntityTypeDescriptors.AsNoTracking()
                .Where(e => e.Id == typeId)
                .Select(static e => e.Key)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return AttributeMapper.ToResponse(entity, entityTypeKey);
    }
}
