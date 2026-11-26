using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to list CMS blocks scoped to a page, layout, or site.</summary>
public sealed record GetCmsBlocksQuery(
    Guid? PageId = null,
    Guid? LayoutId = null,
    Guid? SiteId = null,
    bool IncludeInactive = true
) : IQuery<IReadOnlyList<CmsBlockResponse>>;
