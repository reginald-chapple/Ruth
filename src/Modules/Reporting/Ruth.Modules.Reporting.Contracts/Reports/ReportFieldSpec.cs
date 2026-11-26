namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Input specification for a single column in a Report.</summary>
/// <param name="FieldSelectKey">Key of the IFieldSelectPlugin used to project column values.</param>
/// <param name="ColumnHeaderText">Displayed column header.</param>
/// <param name="SelectionConfigJson">Optional JSON config for the field selector plugin.</param>
/// <param name="SortOrder">Optional sort priority (null means not sorted).</param>
/// <param name="SortDirection">Sort direction when SortOrder is specified.</param>
/// <param name="ShowInGrid">Whether the column is visible in grid/export output.</param>
public sealed record ReportFieldSpec(
    string FieldSelectKey,
    string ColumnHeaderText,
    string? SelectionConfigJson = null,
    int? SortOrder = null,
    ReportSortDirection SortDirection = ReportSortDirection.None,
    bool ShowInGrid = true
);
