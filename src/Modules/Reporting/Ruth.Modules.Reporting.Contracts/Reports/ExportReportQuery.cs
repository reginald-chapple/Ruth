using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Exports a Report as CSV or Excel (.xlsx).</summary>
/// <param name="ReportId">Report identifier.</param>
/// <param name="Format">Export format (Csv or Excel).</param>
/// <param name="MaxRows">Optional row limit override.</param>
public sealed record ExportReportQuery(
    Guid ReportId,
    ReportExportFormat Format = ReportExportFormat.Csv,
    int? MaxRows = null
) : IQuery<ReportingFileExportResponse>;
