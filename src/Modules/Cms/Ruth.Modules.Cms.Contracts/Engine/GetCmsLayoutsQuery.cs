using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to list layouts belonging to a CMS site.</summary>
public sealed record GetCmsLayoutsQuery(Guid SiteId) : IQuery<IReadOnlyList<CmsLayoutResponse>>;
