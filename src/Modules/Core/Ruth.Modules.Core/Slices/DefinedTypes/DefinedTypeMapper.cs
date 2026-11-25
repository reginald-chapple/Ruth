using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal static class DefinedTypeMapper
{
    public static DefinedValueResponse ToValueResponse(DefinedValue item)
    {
        return new DefinedValueResponse(
            item.Id.Value,
            item.DefinedTypeId.Value,
            item.SystemGuid,
            item.Value,
            item.Description,
            item.CategoryId?.Value,
            item.Order,
            item.IsSystem,
            item.IsActive,
            new Dictionary<string, string?>(item.Attributes, StringComparer.OrdinalIgnoreCase)
        );
    }

    public static DefinedTypeDetailResponse ToDetailResponse(DefinedType entity)
    {
        return new DefinedTypeDetailResponse(
            entity.Id.Value,
            entity.SystemGuid,
            entity.Name,
            entity.Description,
            entity.HelpText,
            entity.CategoryId?.Value,
            entity.Order,
            entity.IsSystem,
            entity.IsActive,
            entity.EnableSecurityOnValues,
            entity.CategorizedValuesEnabled,
            [
                .. entity
                    .Values.OrderBy(static v => v.Order)
                    .ThenBy(static v => v.Value, StringComparer.OrdinalIgnoreCase)
                    .Select(ToValueResponse),
            ]
        );
    }
}
