using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to list CMS request filters.</summary>
public sealed record GetRequestFiltersQuery(Guid? SiteId = null, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<RequestFilterResponse>>;
