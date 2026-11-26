using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Query to render a syndication feed (RSS 2.0 or JSON Feed 1.1) for a content channel.</summary>
public sealed record GetContentChannelFeedQuery(
    string ChannelSlug,
    string Format = "rss",
    string BaseOrigin = "https://ruth.local"
) : IQuery<ContentChannelFeedResponse>;
