namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Hierarchical location tree node including nested child locations.</summary>
public sealed record LocationTreeNodeResponse(
    LocationResponse Location,
    IReadOnlyList<LocationTreeNodeResponse> Children
);
