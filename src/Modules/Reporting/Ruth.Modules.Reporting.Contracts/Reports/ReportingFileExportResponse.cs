namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Binary file payload returned by Report export or MergeTemplate generation.</summary>
/// <param name="FileName">Suggested download file name.</param>
/// <param name="ContentType">MIME content type.</param>
/// <param name="Content">Encoded document bytes.</param>
/// <param name="RowCount">Number of data rows included.</param>
public sealed record ReportingFileExportResponse(
    string FileName,
    string ContentType,
    byte[] Content,
    int RowCount
);
