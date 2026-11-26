using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Query to list CMS editorial topics.</summary>
public sealed record GetContentTopicsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<ContentTopicResponse>>;
