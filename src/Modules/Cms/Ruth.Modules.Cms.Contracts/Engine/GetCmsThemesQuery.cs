using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to list CMS themes.</summary>
public sealed record GetCmsThemesQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<CmsThemeResponse>>;
