namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Summary representation of a DefinedType lookup container.</summary>
public sealed record DefinedTypeSummaryResponse(
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
    int ValueCount
);
