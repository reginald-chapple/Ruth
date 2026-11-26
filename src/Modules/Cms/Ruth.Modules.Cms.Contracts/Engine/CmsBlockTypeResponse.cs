namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a registered CMS block type usable inside page/layout/site zones.</summary>
public sealed record CmsBlockTypeResponse(
    Guid Id,
    string Key,
    string Name,
    string Category,
    string? Description,
    bool RequiresInteractivity,
    IReadOnlyDictionary<string, string?> DefaultSettings,
    bool IsSystem
);
