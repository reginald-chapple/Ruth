using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Search.Domain.Collections;

/// <summary>Searchable ContentCollection definition aggregating documents from one or more ISearchDocumentProviders.</summary>
public sealed class ContentCollectionDefinition
    : AggregateRoot<ContentCollectionId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Search.ContentCollection";

    private readonly List<ContentCollectionSource> _sources = [];

    private ContentCollectionDefinition() { }

    private ContentCollectionDefinition(ContentCollectionId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string CollectionKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsPublic { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public bool EnableRequestFilters { get; private set; }
    public bool EnableSegments { get; private set; }
    public DateTimeOffset? LastIndexDateTimeUtc { get; private set; }
    public int LastIndexItemCount { get; private set; }

    public IReadOnlyCollection<ContentCollectionSource> Sources => _sources.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<ContentCollectionDefinition> Create(
        string name,
        string collectionKey,
        string? description = null,
        bool isPublic = true,
        bool enableRequestFilters = false,
        bool enableSegments = false
    )
    {
        var slug = SearchTextNormalizer.NormalizeSlug(collectionKey);
        return string.IsNullOrWhiteSpace(name)
                ? Result.Failure<ContentCollectionDefinition>(
                    SearchErrors.Validation(
                        "Collection.NameRequired",
                        "ContentCollection name is required."
                    )
                )
            : string.IsNullOrWhiteSpace(slug)
                ? Result.Failure<ContentCollectionDefinition>(
                    SearchErrors.Validation(
                        "Collection.KeyInvalid",
                        "ContentCollection key is invalid."
                    )
                )
            : Result.Success(
                new ContentCollectionDefinition(ContentCollectionId.Create())
                {
                    Name = name.Trim(),
                    CollectionKey = slug,
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    IsPublic = isPublic,
                    IsActive = true,
                    EnableRequestFilters = enableRequestFilters,
                    EnableSegments = enableSegments,
                }
            );
    }

    public Result<ContentCollectionSource> AddSource(
        string providerKey,
        string entityTypeKey,
        int occurrencesToShow = 100,
        double customBoostWeight = 1.0,
        string? filterConfigJson = null
    )
    {
        var sourceRes = ContentCollectionSource.Create(
            Id,
            providerKey,
            entityTypeKey,
            occurrencesToShow,
            customBoostWeight,
            filterConfigJson
        );
        if (sourceRes.IsFailure)
        {
            return sourceRes;
        }

        if (
            _sources.Exists(s =>
                string.Equals(
                    s.ProviderKey,
                    sourceRes.Value.ProviderKey,
                    StringComparison.OrdinalIgnoreCase
                )
            )
        )
        {
            return Result.Failure<ContentCollectionSource>(
                SearchErrors.Conflict(
                    "Source.Duplicate",
                    $"Provider '{sourceRes.Value.ProviderKey}' is already attached to this collection."
                )
            );
        }

        _sources.Add(sourceRes.Value);
        return sourceRes;
    }

    public void RecordIndexed(int itemCount, DateTimeOffset indexedUtc)
    {
        LastIndexItemCount = Math.Max(0, itemCount);
        LastIndexDateTimeUtc = indexedUtc.ToUniversalTime();
    }
}
