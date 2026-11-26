namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Modular provider supplying queryable entity records for a Reporting DataView entity type.</summary>
public interface IDataViewSourceProvider
{
    /// <summary>Gets the canonical entity type key handled by this provider (e.g., Ruth.Crm.Person).</summary>
    string EntityTypeKey { get; }

    /// <summary>Gets the display name of the entity source.</summary>
    string DisplayName { get; }

    /// <summary>Gets the list of queryable field keys exposed by this source.</summary>
    IReadOnlyList<string> AvailableFields { get; }

    /// <summary>Loads candidate entity records for DataView AST evaluation and report rendering.</summary>
    Task<IReadOnlyList<DataViewEntityRecord>> LoadRecordsAsync(
        int maxRows,
        CancellationToken cancellationToken = default
    );
}
