using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Domain.FieldTypes;

namespace Ruth.Modules.Core.Domain;

/// <summary>Pure-domain validator normalizing and formatting entity attribute bags against definitions and matrix templates.</summary>
public static class AttributeValueValidator
{
    /// <summary>Validates, normalizes, and formats supplied attribute values against active definitions.</summary>
    public static AttributeValidationResponse Validate(
        IReadOnlyList<AttributeDefinition> definitions,
        IReadOnlyDictionary<string, string?> inputValues,
        IFieldTypeRegistry fieldTypes,
        IReadOnlyDictionary<Guid, AttributeMatrixTemplate>? matrixTemplates = null
    )
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(inputValues);
        ArgumentNullException.ThrowIfNull(fieldTypes);

        var lookup = new Dictionary<string, string?>(inputValues, StringComparer.OrdinalIgnoreCase);
        var normalized = new Dictionary<string, string?>(StringComparer.Ordinal);
        var formatted = new Dictionary<string, string>(StringComparer.Ordinal);
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (
            var definition in definitions.Where(static d => d.IsActive).OrderBy(static d => d.Order)
        )
        {
            ValidateSingleAttribute(
                definition,
                lookup,
                fieldTypes,
                matrixTemplates,
                normalized,
                formatted,
                errors
            );
        }

        return new AttributeValidationResponse(errors.Count == 0, normalized, formatted, errors);
    }

    private static void ValidateSingleAttribute(
        AttributeDefinition definition,
        Dictionary<string, string?> lookup,
        IFieldTypeRegistry fieldTypes,
        IReadOnlyDictionary<Guid, AttributeMatrixTemplate>? matrixTemplates,
        Dictionary<string, string?> normalized,
        Dictionary<string, string> formatted,
        Dictionary<string, string> errors
    )
    {
        if (!fieldTypes.TryGet(definition.FieldTypeKey, out var plugin))
        {
            errors[definition.Key] = $"Unknown field type '{definition.FieldTypeKey}'.";
            return;
        }

        var raw = lookup.TryGetValue(definition.Key, out var supplied)
            ? supplied
            : definition.DefaultValue;

        var check = plugin.ValidateAndNormalize(raw, definition.Qualifiers, definition.IsRequired);
        if (check.IsFailure)
        {
            errors[definition.Key] = check.PrimaryError.Description;
            return;
        }

        var finalValue = check.Value;
        if (
            finalValue is not null
            && string.Equals(plugin.Key, "matrix", StringComparison.OrdinalIgnoreCase)
            && TryResolveMatrixTemplate(definition, matrixTemplates, out var template)
        )
        {
            var matrixCheck = template.ValidateAndNormalizeRows(finalValue, fieldTypes);
            if (matrixCheck.IsFailure)
            {
                errors[definition.Key] = matrixCheck.PrimaryError.Description;
                return;
            }

            finalValue = matrixCheck.Value;
        }

        normalized[definition.Key] = finalValue;
        formatted[definition.Key] = plugin.FormatDisplayValue(finalValue, definition.Qualifiers);
    }

    private static bool TryResolveMatrixTemplate(
        AttributeDefinition definition,
        IReadOnlyDictionary<Guid, AttributeMatrixTemplate>? matrixTemplates,
        out AttributeMatrixTemplate template
    )
    {
        template = null!;
        return matrixTemplates is not null
            && definition.Qualifiers.TryGetValue("attributeMatrixTemplateId", out var rawId)
            && Guid.TryParse(rawId, out var templateId)
            && matrixTemplates.TryGetValue(templateId, out template!);
    }
}
