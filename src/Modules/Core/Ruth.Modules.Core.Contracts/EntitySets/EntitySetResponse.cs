namespace Ruth.Modules.Core.Contracts.EntitySets;

/// <summary>Ephemeral or saved bulk selection set of entity identifiers.</summary>
public sealed record EntitySetResponse(
    Guid Id,
    Guid? ParentEntitySetId,
    string? Name,
    Guid? EntityTypeId,
    DateTimeOffset? ExpireOnUtc,
    bool IsExpired,
    Guid? PurposeValueId,
    string? Note,
    int Order,
    IReadOnlyList<EntitySetItemDto> Items,
    DateTimeOffset CreatedOnUtc
);
