namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Detailed representation of a DefinedType including its ordered DefinedValues.</summary>
public sealed record DefinedTypeDetailResponse(
    Guid Id,
    Guid SystemGuid,
    string Name,
    string? Description,
    string? HelpText,
    Guid? CategoryId,
    int Order,
    bool IsSystem,
    bool IsActive,
    bool EnableSecurityOnValues,
    bool CategorizedValuesEnabled,
    IReadOnlyList<DefinedValueResponse> Values
);
