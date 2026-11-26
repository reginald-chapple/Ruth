using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Contracts.Collections;

/// <summary>Lists configured ContentCollections.</summary>
/// <param name="IncludeInactive">Whether to include inactive collections.</param>
public sealed record GetContentCollectionsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<ContentCollectionResponse>>;
