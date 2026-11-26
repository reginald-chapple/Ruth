namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a configured CMS request filter.</summary>
public sealed record RequestFilterResponse(
    Guid Id,
    string Name,
    string Key,
    Guid? SiteId,
    RequestFilterMode FilterMode,
    IReadOnlyList<RequestFilterRuleSpec> Rules,
    bool IsActive
);
