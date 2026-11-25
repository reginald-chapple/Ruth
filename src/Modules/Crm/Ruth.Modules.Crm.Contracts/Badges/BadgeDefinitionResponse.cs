namespace Ruth.Modules.Crm.Contracts.Badges;

/// <summary>Represents a configured CRM badge definition.</summary>
public sealed record BadgeDefinitionResponse(
    Guid Id,
    string Name,
    string? Description,
    string BadgeComponentKey,
    string EntityTypeKey,
    bool IsActive,
    int Order,
    IReadOnlyDictionary<string, string?> Settings
);
