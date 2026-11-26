using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Query to list items in a content channel, optionally filtered to currently published items.</summary>
public sealed record GetContentChannelItemsQuery(
    Guid? ContentChannelId = null,
    string? ChannelSlug = null,
    Guid? TopicId = null,
    bool PublishedOnly = false
) : IQuery<IReadOnlyList<ContentChannelItemResponse>>;
