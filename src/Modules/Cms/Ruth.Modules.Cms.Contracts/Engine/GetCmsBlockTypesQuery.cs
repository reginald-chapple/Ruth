using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to list all registered CMS block types.</summary>
public sealed record GetCmsBlockTypesQuery : IQuery<IReadOnlyList<CmsBlockTypeResponse>>;
