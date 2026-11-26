namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Represents a publishable CMS content channel item with slug, schedule, topics, and associations.</summary>
public sealed record ContentChannelItemResponse(
    Guid Id,
    Guid ContentChannelId,
    string Title,
    string Slug,
    string? Summary,
    string Content,
    int Priority,
    ContentChannelItemStatus Status,
    Guid? ApprovedByPersonId,
    DateTimeOffset? ApprovedDateTimeUtc,
    DateTimeOffset StartDateTimeUtc,
    DateTimeOffset? ExpireDateTimeUtc,
    int Order,
    Guid? AuthorPersonId,
    Guid? PrimaryTopicId,
    IReadOnlyList<Guid> TopicIds,
    IReadOnlyList<Guid> AssociatedChildItemIds,
    IReadOnlyDictionary<string, string?> AttributeValues
);
