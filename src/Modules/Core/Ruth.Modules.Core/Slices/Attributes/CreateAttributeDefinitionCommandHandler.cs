using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.FieldTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Attributes;

internal sealed class CreateAttributeDefinitionCommandHandler(
    CoreDbContext dbContext,
    IFieldTypeRegistry fieldTypes,
    ICoreReadCache cache
) : ICommandHandler<CreateAttributeDefinitionCommand, AttributeDefinitionResponse>
{
    public async Task<Result<AttributeDefinitionResponse>> HandleAsync(
        CreateAttributeDefinitionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var scopeResult = await ResolveEntityScopeAsync(command.EntityTypeId, cancellationToken);
        if (scopeResult.IsFailure)
        {
            return Result.Failure<AttributeDefinitionResponse>(scopeResult.Errors);
        }

        var (entityTypeId, entityTypeKey) = scopeResult.Value;
        var normalizedKey = command.Key?.Trim().ToUpperInvariant() ?? string.Empty;
        var qualCol = string.IsNullOrWhiteSpace(command.EntityTypeQualifierColumn)
            ? null
            : command.EntityTypeQualifierColumn.Trim();
        var qualVal = string.IsNullOrWhiteSpace(command.EntityTypeQualifierValue)
            ? null
            : command.EntityTypeQualifierValue.Trim();

        var duplicateExists = await dbContext.AttributeDefinitions.AnyAsync(
            a =>
                a.EntityTypeId == entityTypeId
                && a.EntityTypeQualifierColumn == qualCol
                && a.EntityTypeQualifierValue == qualVal
                && a.NormalizedKey == normalizedKey,
            cancellationToken
        );
        if (duplicateExists)
        {
            return CoreErrors.AttributeDuplicateKey;
        }

        var created = AttributeDefinition.Create(
            fieldTypes,
            entityTypeId,
            qualCol,
            qualVal,
            command.Key ?? string.Empty,
            command.Name,
            command.Description,
            command.FieldTypeKey,
            command.IsRequired,
            command.IsGridColumn,
            command.IsMultiValue,
            isSystem: false,
            command.DefaultValue,
            command.GlobalValue,
            command.Order,
            command.CategoryId.HasValue ? CategoryId.From(command.CategoryId.Value) : null,
            command.Qualifiers
        );
        if (created.IsFailure)
        {
            return Result.Failure<AttributeDefinitionResponse>(created.Errors);
        }

        _ = dbContext.AttributeDefinitions.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.AttributesBucket, cancellationToken);
        return AttributeMapper.ToResponse(created.Value, entityTypeKey);
    }

    private async Task<Result<(EntityTypeDescriptorId? Id, string? Key)>> ResolveEntityScopeAsync(
        Guid? rawEntityTypeId,
        CancellationToken cancellationToken
    )
    {
        if (!rawEntityTypeId.HasValue || rawEntityTypeId.Value == Guid.Empty)
        {
            return (null, null);
        }

        var id = EntityTypeDescriptorId.From(rawEntityTypeId.Value);
        var descriptor = await dbContext
            .EntityTypeDescriptors.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return descriptor is null
            ? CoreErrors.EntityTypeNotFound
            : Result.Success<(EntityTypeDescriptorId? Id, string? Key)>((id, descriptor.Key));
    }
}
