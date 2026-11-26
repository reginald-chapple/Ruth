using Ruth.Modules.Search.Contracts.Collections;
using Ruth.Modules.Search.Domain.Collections;

namespace Ruth.Modules.Search.Slices;

internal static class SearchDtoMapper
{
    public static ContentCollectionResponse ToResponse(ContentCollectionDefinition collection)
    {
        List<ContentCollectionSourceResponse> sources =
        [
            .. collection
                .Sources.OrderBy(static s => s.ProviderKey, StringComparer.OrdinalIgnoreCase)
                .Select(static s => new ContentCollectionSourceResponse(
                    s.Id.Value,
                    s.ProviderKey,
                    s.EntityTypeKey,
                    s.OccurrencesToShow,
                    s.CustomBoostWeight,
                    s.FilterConfigJson,
                    s.IsActive
                )),
        ];

        return new ContentCollectionResponse(
            collection.Id.Value,
            collection.Name,
            collection.CollectionKey,
            collection.Description,
            collection.IsPublic,
            collection.IsActive,
            collection.EnableRequestFilters,
            collection.EnableSegments,
            collection.LastIndexDateTimeUtc,
            collection.LastIndexItemCount,
            sources
        );
    }
}
