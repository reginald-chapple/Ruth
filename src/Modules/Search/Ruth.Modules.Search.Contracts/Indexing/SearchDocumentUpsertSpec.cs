namespace Ruth.Modules.Search.Contracts.Indexing;

/// <summary>Canonical document payload indexed into PostgreSQL tsvector + pg_trgm full-text search.</summary>
/// <param name="SourceKey">Provider or projection source key (e.g., crm.people, lms.courses).</param>
/// <param name="ModuleName">Owning module name (Crm, Groups, Finance, Events, Commerce, Engagement, Workflow, Cms, Lms).</param>
/// <param name="EntityTypeKey">Canonical EntityType key (e.g., Ruth.Crm.Person).</param>
/// <param name="EntityId">Primary entity identifier.</param>
/// <param name="Title">Weight A title text.</param>
/// <param name="Subtitle">Weight B subtitle text.</param>
/// <param name="Summary">Weight C summary snippet text.</param>
/// <param name="BodyText">Weight D full body text.</param>
/// <param name="Tags">Weight B keyword tags.</param>
/// <param name="UrlPath">Target deep-link route in Staff RMS or Public Portal.</param>
/// <param name="IsPublic">Whether visible to anonymous/public CMS visitors.</param>
/// <param name="SecurityEntityTypeKey">Optional ACL entity type key override (defaults to EntityTypeKey).</param>
/// <param name="SecurityEntityId">Optional ACL entity ID override (defaults to EntityId).</param>
/// <param name="Facets">Optional key-value facets (e.g., BranchId, Category, Status).</param>
/// <param name="ContentCollectionId">Optional specific ContentCollection scope (null for universal Spotlight index).</param>
public sealed record SearchDocumentUpsertSpec(
    string SourceKey,
    string ModuleName,
    string EntityTypeKey,
    Guid EntityId,
    string Title,
    string? Subtitle = null,
    string? Summary = null,
    string? BodyText = null,
    IReadOnlyList<string>? Tags = null,
    string? UrlPath = null,
    bool IsPublic = false,
    string? SecurityEntityTypeKey = null,
    Guid? SecurityEntityId = null,
    IReadOnlyDictionary<string, string>? Facets = null,
    Guid? ContentCollectionId = null
);
