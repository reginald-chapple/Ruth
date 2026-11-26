namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a CMS site layout with named content zones.</summary>
public sealed record CmsLayoutResponse(
    Guid Id,
    Guid SiteId,
    string Name,
    string Slug,
    string? Description,
    string FileName,
    IReadOnlyList<string> Zones,
    bool IsActive
);
