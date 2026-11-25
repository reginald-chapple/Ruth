using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Gets the hierarchical tree of named/physical locations.</summary>
public sealed record GetLocationTreeQuery(
    Guid? RootLocationId = null,
    bool NamedOnly = true,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<LocationTreeNodeResponse>>;
