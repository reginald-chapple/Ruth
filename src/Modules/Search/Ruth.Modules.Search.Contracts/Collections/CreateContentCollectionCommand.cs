using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Contracts.Collections;

/// <summary>Creates a ContentCollection with one or more ISearchDocumentProvider sources.</summary>
/// <param name="Name">Collection name.</param>
/// <param name="CollectionKey">Unique slug key.</param>
/// <param name="Sources">Configured provider sources.</param>
/// <param name="Description">Optional description.</param>
/// <param name="IsPublic">Whether accessible on public CMS/portal search.</param>
/// <param name="EnableRequestFilters">Enable request filters.</param>
/// <param name="EnableSegments">Enable segment filtering.</param>
public sealed record CreateContentCollectionCommand(
    string Name,
    string CollectionKey,
    IReadOnlyList<ContentCollectionSourceSpec>? Sources = null,
    string? Description = null,
    bool IsPublic = true,
    bool EnableRequestFilters = false,
    bool EnableSegments = false
) : ICommand<ContentCollectionResponse>;
