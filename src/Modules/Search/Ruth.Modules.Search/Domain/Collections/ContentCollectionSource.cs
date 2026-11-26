using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Search.Domain.Collections;

/// <summary>Source provider binding inside a ContentCollection.</summary>
public sealed class ContentCollectionSource : Entity<ContentCollectionSourceId>
{
    private ContentCollectionSource() { }

    private ContentCollectionSource(ContentCollectionSourceId id)
        : base(id) { }

    public ContentCollectionId ContentCollectionId { get; private set; }
    public string ProviderKey { get; private set; } = string.Empty;
    public string EntityTypeKey { get; private set; } = string.Empty;
    public int OccurrencesToShow { get; private set; } = 100;
    public double CustomBoostWeight { get; private set; } = 1.0;
    public string? FilterConfigJson { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Result<ContentCollectionSource> Create(
        ContentCollectionId collectionId,
        string providerKey,
        string entityTypeKey,
        int occurrencesToShow = 100,
        double customBoostWeight = 1.0,
        string? filterConfigJson = null
    )
    {
        return string.IsNullOrWhiteSpace(providerKey)
                ? Result.Failure<ContentCollectionSource>(
                    SearchErrors.Validation(
                        "Source.ProviderKeyRequired",
                        "ProviderKey is required."
                    )
                )
            : occurrencesToShow <= 0
                ? Result.Failure<ContentCollectionSource>(
                    SearchErrors.Validation(
                        "Source.OccurrencesInvalid",
                        "OccurrencesToShow must be greater than zero."
                    )
                )
            : Result.Success(
                new ContentCollectionSource(ContentCollectionSourceId.Create())
                {
                    ContentCollectionId = collectionId,
                    ProviderKey = providerKey.Trim().ToLowerInvariant(),
                    EntityTypeKey = string.IsNullOrWhiteSpace(entityTypeKey)
                        ? providerKey.Trim()
                        : entityTypeKey.Trim(),
                    OccurrencesToShow = occurrencesToShow,
                    CustomBoostWeight = Math.Clamp(customBoostWeight, 0.1, 10.0),
                    FilterConfigJson = string.IsNullOrWhiteSpace(filterConfigJson)
                        ? null
                        : filterConfigJson.Trim(),
                    IsActive = true,
                }
            );
    }
}
