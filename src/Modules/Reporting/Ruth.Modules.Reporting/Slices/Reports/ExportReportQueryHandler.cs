using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.Reports;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Reports;

internal sealed class ExportReportQueryHandler(
    IQueryHandler<ExecuteReportQuery, ReportExecutionResponse> executeReport
) : IQueryHandler<ExportReportQuery, ReportingFileExportResponse>
{
    public async Task<Result<ReportingFileExportResponse>> HandleAsync(
        ExportReportQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var executed = await executeReport.HandleAsync(
            new ExecuteReportQuery(query.ReportId, query.MaxRows),
            cancellationToken
        );
        if (executed.IsFailure)
        {
            return Result.Failure<ReportingFileExportResponse>(executed.PrimaryError);
        }

        var data = executed.Value;
        var baseName = ReportingTextNormalizer.SanitizeFileName(data.ReportName, "report");

        if (query.Format == ReportExportFormat.Excel)
        {
            var bytes = ReportingDocumentPackager.BuildExcelXlsx(
                data.ReportName,
                data.Columns,
                data.Rows
            );
            return Result.Success(
                new ReportingFileExportResponse(
                    $"{baseName}.xlsx",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    bytes,
                    data.TotalRows
                )
            );
        }

        var csvBytes = ReportingDocumentPackager.BuildCsv(data.Columns, data.Rows);
        return Result.Success(
            new ReportingFileExportResponse(
                $"{baseName}.csv",
                "text/csv; charset=utf-8",
                csvBytes,
                data.TotalRows
            )
        );
    }
}
