using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.EntitySets;

/// <summary>Creates an ephemeral (expiring) or saved (permanent) entity bulk selection set.</summary>
public sealed record CreateEntitySetCommand(
    string? Name,
    Guid? EntityTypeId,
    IReadOnlyList<Guid> EntityIds,
    int? ExpireInMinutes = 20,
    Guid? ParentEntitySetId = null,
    Guid? PurposeValueId = null,
    string? Note = null,
    int Order = 0
) : ICommand<EntitySetResponse>;
