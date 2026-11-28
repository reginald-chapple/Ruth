using System.Text.Json;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Domain.Querying;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Search.Domain.Indexing;

/// <summary>
/// Indexed search document materialized in search.search_documents with weighted PostgreSQL
/// tsvector (A=Title, B=Subtitle+Tags, C=Summary, D=BodyText) and pg_trgm trigram indexes.
/// </summary>
public sealed class SearchDocument : AggregateRoot<SearchDocumentId>, IAuditableEntity
{
    public const string EntityTypeKeyName = "Ruth.Search.SearchDocument";

    private SearchDocument() { }

    private SearchDocument(SearchDocumentId id)
        : base(id) { }

    public ContentCollectionId? ContentCollectionId { get; private set; }
    public string SourceKey { get; private set; } = string.Empty;
    public string ModuleName { get; private set; } = string.Empty;
    public string EntityTypeKey { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }
    public string? Summary { get; private set; }
    public string? BodyText { get; private set; }
    public string TagsText { get; private set; } = string.Empty;
    public string FacetsJson { get; private set; } = "{}";
    public string? UrlPath { get; private set; }
    public bool IsPublic { get; private set; }
    public string SecurityEntityTypeKey { get; private set; } = string.Empty;
    public Guid SecurityEntityId { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public DateTimeOffset IndexedDateTimeUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public static Result<SearchDocument> Create(
        SearchDocumentUpsertSpec spec,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var validation = ValidateSpec(spec);
        if (validation.IsFailure)
        {
            return Result.Failure<SearchDocument>(validation.PrimaryError);
        }

        var tagsText = FormatTags(spec.Tags);
        var facetsJson = SerializeFacets(spec.Facets);
        var title = spec.Title.Trim();
        var subtitle = TrimOrNull(spec.Subtitle);
        var summary = TrimOrNull(spec.Summary);
        var body = TrimOrNull(spec.BodyText);
        var url = SearchSnippetSanitizer.SanitizeUrlPath(spec.UrlPath);
        var hash = SearchTextNormalizer.ComputeContentHash(
            title,
            subtitle,
            summary,
            body,
            tagsText,
            url,
            spec.IsPublic,
            facetsJson
        );

        ContentCollectionId? collId =
            spec.ContentCollectionId is { } cid && cid != Guid.Empty
                ? Domain.ContentCollectionId.From(cid)
                : null;

        return Result.Success(
            new SearchDocument(SearchDocumentId.Create())
            {
                ContentCollectionId = collId,
                SourceKey = spec.SourceKey.Trim().ToLowerInvariant(),
                ModuleName = spec.ModuleName.Trim(),
                EntityTypeKey = spec.EntityTypeKey.Trim(),
                EntityId = spec.EntityId,
                Title = title,
                Subtitle = subtitle,
                Summary = summary,
                BodyText = body,
                TagsText = tagsText,
                FacetsJson = facetsJson,
                UrlPath = url,
                IsPublic = spec.IsPublic,
                SecurityEntityTypeKey = string.IsNullOrWhiteSpace(spec.SecurityEntityTypeKey)
                    ? spec.EntityTypeKey.Trim()
                    : spec.SecurityEntityTypeKey.Trim(),
                SecurityEntityId =
                    spec.SecurityEntityId is { } sid && sid != Guid.Empty ? sid : spec.EntityId,
                ContentHash = hash,
                IndexedDateTimeUtc = nowUtc.ToUniversalTime(),
            }
        );
    }

    public bool ApplyUpdate(SearchDocumentUpsertSpec spec, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var tagsText = FormatTags(spec.Tags);
        var facetsJson = SerializeFacets(spec.Facets);
        var title = spec.Title.Trim();
        var subtitle = TrimOrNull(spec.Subtitle);
        var summary = TrimOrNull(spec.Summary);
        var body = TrimOrNull(spec.BodyText);
        var url = SearchSnippetSanitizer.SanitizeUrlPath(spec.UrlPath);
        var hash = SearchTextNormalizer.ComputeContentHash(
            title,
            subtitle,
            summary,
            body,
            tagsText,
            url,
            spec.IsPublic,
            facetsJson
        );

        if (string.Equals(ContentHash, hash, StringComparison.Ordinal))
        {
            return false;
        }

        Title = title;
        Subtitle = subtitle;
        Summary = summary;
        BodyText = body;
        TagsText = tagsText;
        FacetsJson = facetsJson;
        UrlPath = url;
        IsPublic = spec.IsPublic;
        SecurityEntityTypeKey = string.IsNullOrWhiteSpace(spec.SecurityEntityTypeKey)
            ? spec.EntityTypeKey.Trim()
            : spec.SecurityEntityTypeKey.Trim();
        SecurityEntityId =
            spec.SecurityEntityId is { } sid && sid != Guid.Empty ? sid : spec.EntityId;
        ContentHash = hash;
        IndexedDateTimeUtc = nowUtc.ToUniversalTime();
        return true;
    }

    private static Result ValidateSpec(SearchDocumentUpsertSpec spec)
    {
        return string.IsNullOrWhiteSpace(spec.SourceKey)
                ? Result.Failure(
                    SearchErrors.Validation("Document.SourceKeyRequired", "SourceKey is required.")
                )
            : string.IsNullOrWhiteSpace(spec.ModuleName)
                ? Result.Failure(
                    SearchErrors.Validation(
                        "Document.ModuleNameRequired",
                        "ModuleName is required."
                    )
                )
            : string.IsNullOrWhiteSpace(spec.EntityTypeKey)
                ? Result.Failure(
                    SearchErrors.Validation(
                        "Document.EntityTypeRequired",
                        "EntityTypeKey is required."
                    )
                )
            : spec.EntityId == Guid.Empty
                ? Result.Failure(
                    SearchErrors.Validation("Document.EntityIdRequired", "EntityId is required.")
                )
            : string.IsNullOrWhiteSpace(spec.Title)
                ? Result.Failure(
                    SearchErrors.Validation("Document.TitleRequired", "Title is required.")
                )
            : Result.Success();
    }

    private static string FormatTags(IReadOnlyList<string>? tags)
    {
        return tags is not { Count: > 0 }
            ? string.Empty
            : string.Join(
                ' ',
                tags.Where(static t => !string.IsNullOrWhiteSpace(t))
                    .Select(static t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            );
    }

    private static string SerializeFacets(IReadOnlyDictionary<string, string>? facets)
    {
        return facets is not { Count: > 0 } ? "{}" : JsonSerializer.Serialize(facets);
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
