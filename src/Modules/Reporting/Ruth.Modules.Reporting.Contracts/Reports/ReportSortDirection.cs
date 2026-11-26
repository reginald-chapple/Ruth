namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Sort direction applied to a ReportField column.</summary>
public enum ReportSortDirection
{
    /// <summary>Unsorted column.</summary>
    None = 0,

    /// <summary>Ascending sort order.</summary>
    Ascending = 1,

    /// <summary>Descending sort order.</summary>
    Descending = 2,
}
