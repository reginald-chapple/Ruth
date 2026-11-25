namespace Ruth.Modules.Core.Contracts.EntitySets;

/// <summary>Individual entity member within an EntitySet.</summary>
public sealed record EntitySetItemDto(
    Guid EntityId,
    int Order,
    string? AdditionalMergeValuesJson = null
);
