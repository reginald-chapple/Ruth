using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to list pages for a CMS site.</summary>
public sealed record GetCmsPagesQuery(Guid SiteId, bool IncludeUnpublished = true)
    : IQuery<IReadOnlyList<CmsPageResponse>>;
