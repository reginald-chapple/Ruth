using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Executes a Report and returns projected tabular rows.</summary>
/// <param name="ReportId">Report identifier.</param>
/// <param name="MaxRows">Optional row limit override.</param>
public sealed record ExecuteReportQuery(Guid ReportId, int? MaxRows = null)
    : IQuery<ReportExecutionResponse>;
