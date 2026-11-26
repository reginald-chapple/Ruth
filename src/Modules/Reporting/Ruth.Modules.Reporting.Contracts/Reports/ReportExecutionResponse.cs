namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Tabular output of running a Report.</summary>
/// <param name="ReportId">Report identifier.</param>
/// <param name="ReportName">Report title.</param>
/// <param name="Columns">Ordered column headers.</param>
/// <param name="Rows">Tabular string-formatted rows aligned with Columns.</param>
/// <param name="TotalRows">Number of rows returned.</param>
/// <param name="ExecutionMilliseconds">Execution duration in milliseconds.</param>
public sealed record ReportExecutionResponse(
    Guid ReportId,
    string ReportName,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows,
    int TotalRows,
    int ExecutionMilliseconds
);
