using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Command to associate a parent content channel item with a related child item.</summary>
public sealed record AssociateContentChannelItemsCommand(
    Guid ParentItemId,
    Guid ChildItemId,
    int Order = 0
) : ICommand<ContentChannelItemResponse>;
