namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Represents a syndication feed payload (RSS 2.0 XML or JSON Feed 1.1) for a content channel.</summary>
public sealed record ContentChannelFeedResponse(
    Guid ContentChannelId,
    string ChannelSlug,
    string Format,
    string ContentType,
    string Payload,
    int ItemCount
);
