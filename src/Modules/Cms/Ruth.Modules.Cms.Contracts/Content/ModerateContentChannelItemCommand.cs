using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Command to approve or deny a content channel item.</summary>
public sealed record ModerateContentChannelItemCommand(
    Guid ItemId,
    ContentChannelItemStatus Status,
    Guid? ApprovedByPersonId = null
) : ICommand<ContentChannelItemResponse>;
