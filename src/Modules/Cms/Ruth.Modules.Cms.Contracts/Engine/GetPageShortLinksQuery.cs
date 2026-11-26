using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to list CMS shortlinks optionally scoped by site.</summary>
public sealed record GetPageShortLinksQuery(Guid? SiteId = null)
    : IQuery<IReadOnlyList<PageShortLinkResponse>>;
