using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Slices.Attributes;

internal static class AttributeMapper
{
    public static AttributeDefinitionResponse ToResponse(
        AttributeDefinition entity,
        string? entityTypeKey
    )
    {
        return new AttributeDefinitionResponse(
            entity.Id.Value,
            entity.SystemGuid,
            entity.EntityTypeId?.Value,
            entityTypeKey,
            entity.EntityTypeQualifierColumn,
            entity.EntityTypeQualifierValue,
            entity.Key,
            entity.Name,
            entity.Description,
            entity.FieldTypeKey,
            entity.IsRequired,
            entity.IsGridColumn,
            entity.IsMultiValue,
            entity.IsSystem,
            entity.IsActive,
            entity.DefaultValue,
            entity.GlobalValue,
            entity.Order,
            entity.CategoryId?.Value,
            new Dictionary<string, string>(entity.Qualifiers, StringComparer.OrdinalIgnoreCase)
        );
    }
}
