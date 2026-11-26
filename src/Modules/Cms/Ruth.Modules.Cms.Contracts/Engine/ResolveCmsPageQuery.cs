using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to resolve a CMS page by host domain (or site slug) and route path.</summary>
public sealed record ResolveCmsPageQuery(
    string? Host = null,
    string? SiteSlug = null,
    string? Path = null,
    IReadOnlyDictionary<string, string>? QueryParameters = null,
    bool IncludeUnpublished = false
) : IQuery<ResolvedCmsPageResponse>;
