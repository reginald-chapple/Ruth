using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Tags;

/// <summary>Removes a tag from an entity.</summary>
public sealed record UntagEntityCommand(
    Guid TagId,
    Guid EntityTypeId,
    Guid EntityId,
    Guid? RequestingUserId = null
) : ICommand<bool>;
