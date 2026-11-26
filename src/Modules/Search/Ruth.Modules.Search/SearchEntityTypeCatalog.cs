using Ruth.Modules.Search.Domain;
using Ruth.Modules.Search.Domain.Collections;
using Ruth.Modules.Search.Domain.Indexing;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Search;

internal static class SearchEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        new(
            SearchSystemGuids.ContentCollection,
            ContentCollectionDefinition.EntityTypeKeyName,
            nameof(ContentCollectionDefinition),
            "Content Collection",
            "Search",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            SearchSystemGuids.SearchDocument,
            SearchDocument.EntityTypeKeyName,
            nameof(SearchDocument),
            "Search Document",
            "Search",
            IsEntity: true,
            IsSecured: false,
            SupportsAttributes: false
        ),
    ];

    public static IReadOnlyList<EntityTypeRegistration> GetAll()
    {
        return _all;
    }
}
