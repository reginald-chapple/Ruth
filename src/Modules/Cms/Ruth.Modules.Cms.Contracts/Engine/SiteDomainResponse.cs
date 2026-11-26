namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a host domain bound to a CMS Site.</summary>
public sealed record SiteDomainResponse(
    Guid Id,
    Guid SiteId,
    string Domain,
    int Order,
    bool IsPrimary
);
