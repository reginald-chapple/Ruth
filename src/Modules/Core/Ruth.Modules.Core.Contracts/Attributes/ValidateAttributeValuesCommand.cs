using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Attributes;

/// <summary>Validates, normalizes, and formats attribute values against matching attribute definitions and matrix templates.</summary>
public sealed record ValidateAttributeValuesCommand(
    string EntityTypeKey,
    string? QualifierColumn,
    string? QualifierValue,
    IReadOnlyDictionary<string, string?> Values
) : ICommand<AttributeValidationResponse>;
