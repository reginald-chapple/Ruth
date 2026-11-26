using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Command to create an editorial topic for classifying CMS content.</summary>
public sealed record CreateContentTopicCommand(
    string Name,
    string Slug,
    string? Description = null,
    Guid? ParentTopicId = null,
    int Order = 0
) : ICommand<ContentTopicResponse>;
