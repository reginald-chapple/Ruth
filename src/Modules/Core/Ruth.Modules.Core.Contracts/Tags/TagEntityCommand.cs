using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Tags;

/// <summary>Applies a tag to an entity (idempotent).</summary>
public sealed record TagEntityCommand(
    Guid TagId,
    Guid EntityTypeId,
    Guid EntityId,
    Guid? RequestingUserId = null,
    int Quantity = 1
) : ICommand<bool>;
