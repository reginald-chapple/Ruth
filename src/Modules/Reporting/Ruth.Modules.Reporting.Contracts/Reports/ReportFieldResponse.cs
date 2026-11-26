namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Persisted column definition belonging to a Report.</summary>
/// <param name="Id">ReportField identifier.</param>
/// <param name="FieldSelectKey">Plugin key.</param>
/// <param name="ColumnHeaderText">Column header label.</param>
/// <param name="ColumnOrder">Zero-based display order.</param>
/// <param name="SelectionConfigJson">Optional JSON configuration.</param>
/// <param name="SortOrder">Optional sort priority.</param>
/// <param name="SortDirection">Sort direction.</param>
/// <param name="ShowInGrid">Whether visible in grid and export.</param>
public sealed record ReportFieldResponse(
    Guid Id,
    string FieldSelectKey,
    string ColumnHeaderText,
    int ColumnOrder,
    string? SelectionConfigJson,
    int? SortOrder,
    ReportSortDirection SortDirection,
    bool ShowInGrid
);
