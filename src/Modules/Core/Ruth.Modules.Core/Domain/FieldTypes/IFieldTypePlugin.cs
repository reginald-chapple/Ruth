using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.FieldTypes;

/// <summary>Modular strategy contract for typed attribute validation, normalization, and formatting.</summary>
public interface IFieldTypePlugin
{
    /// <summary>Gets the unique lowercase plugin key (for example, text, integer, currency, matrix).</summary>
    string Key { get; }

    /// <summary>Gets the deterministic SystemGuid for this field type.</summary>
    Guid SystemGuid { get; }

    /// <summary>Gets the human-readable field type name.</summary>
    string Name { get; }

    /// <summary>Gets a brief description of this field type.</summary>
    string Description { get; }

    /// <summary>Gets the UI editor kind used by dynamic Blazor editors.</summary>
    FieldEditorKind EditorKind { get; }

    /// <summary>Gets the supported configuration qualifier definitions.</summary>
    IReadOnlyList<FieldQualifierDefinition> Qualifiers { get; }

    /// <summary>Validates and normalizes a raw input value into its canonical storage string.</summary>
    Result<string?> ValidateAndNormalize(
        string? rawValue,
        IReadOnlyDictionary<string, string> qualifiers,
        bool isRequired
    );

    /// <summary>Formats a stored attribute value for human-readable summary display.</summary>
    string FormatDisplayValue(string? storedValue, IReadOnlyDictionary<string, string> qualifiers);
}
