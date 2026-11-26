namespace Ruth.Modules.Search.Contracts.Indexing;

/// <summary>Modular search document provider supplying indexable records from a Bounded Context.</summary>
public interface ISearchDocumentProvider
{
    /// <summary>Gets the canonical provider key (e.g., crm.people, groups.groups, lms.courses).</summary>
    string ProviderKey { get; }

    /// <summary>Gets the owning module display name (e.g., Crm, Groups, Finance, Lms).</summary>
    string ModuleName { get; }

    /// <summary>Gets the canonical entity type key indexed by this provider.</summary>
    string EntityTypeKey { get; }

    /// <summary>Loads documents from the owning module for deterministic PostgreSQL full-text indexing.</summary>
    Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    );
}
