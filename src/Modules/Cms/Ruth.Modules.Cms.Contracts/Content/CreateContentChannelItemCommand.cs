using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Command to create a publishable item inside a content channel.</summary>
public sealed record CreateContentChannelItemCommand(
    Guid ContentChannelId,
    string Title,
    string Slug,
    string Content,
    string? Summary = null,
    int Priority = 0,
    DateTimeOffset? StartDateTimeUtc = null,
    DateTimeOffset? ExpireDateTimeUtc = null,
    int Order = 0,
    Guid? AuthorPersonId = null,
    Guid? PrimaryTopicId = null,
    IReadOnlyList<Guid>? TopicIds = null,
    IReadOnlyDictionary<string, string?>? AttributeValues = null
) : ICommand<ContentChannelItemResponse>;
