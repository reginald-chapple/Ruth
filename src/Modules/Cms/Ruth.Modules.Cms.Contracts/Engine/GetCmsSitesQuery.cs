using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to list CMS sites.</summary>
public sealed record GetCmsSitesQuery(CmsSiteType? SiteType = null, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<CmsSiteResponse>>;
