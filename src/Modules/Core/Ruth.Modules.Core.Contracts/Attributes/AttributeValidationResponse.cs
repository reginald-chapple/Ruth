namespace Ruth.Modules.Core.Contracts.Attributes;

/// <summary>Result of validating and formatting a bag of entity attribute values against applicable definitions.</summary>
public sealed record AttributeValidationResponse(
    bool IsValid,
    IReadOnlyDictionary<string, string?> NormalizedValues,
    IReadOnlyDictionary<string, string> FormattedValues,
    IReadOnlyDictionary<string, string> FieldErrors
);
