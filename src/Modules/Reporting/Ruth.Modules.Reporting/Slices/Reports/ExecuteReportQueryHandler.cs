using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.DataViews;
using Ruth.Modules.Reporting.Domain.Reports;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.Modules.Reporting.Infrastructure.DataViews;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Reports;

internal sealed class ExecuteReportQueryHandler(
    ReportingDbContext db,
    ReportingDataViewExecutionEngine dataViewEngine,
    IEnumerable<IFieldSelectPlugin> fieldPlugins
) : IQueryHandler<ExecuteReportQuery, ReportExecutionResponse>
{
    public async Task<Result<ReportExecutionResponse>> HandleAsync(
        ExecuteReportQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = ReportId.From(query.ReportId);
        var report = await db
            .Reports.AsNoTracking()
            .Include(static r => r.Fields)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return Result.Failure<ReportExecutionResponse>(
                ReportingErrors.NotFound("Report.NotFound", "Report was not found.")
            );
        }

        var sw = Stopwatch.StartNew();
        var maxRows = Math.Clamp(query.MaxRows ?? report.FetchTop ?? 500, 1, 5000);
        var records = await LoadReportRecordsAsync(report, maxRows, cancellationToken);
        var pluginMap = fieldPlugins.ToDictionary(
            static p => p.Key,
            StringComparer.OrdinalIgnoreCase
        );

        var orderedFields = report
            .Fields.Where(static f => f.ShowInGrid)
            .OrderBy(static f => f.ColumnOrder)
            .ToList();
        var projected = ProjectAndSortRows(records, orderedFields, pluginMap);
        sw.Stop();

        List<string> columns = [.. orderedFields.Select(static f => f.ColumnHeaderText)];
        return Result.Success(
            new ReportExecutionResponse(
                report.Id.Value,
                report.Name,
                columns,
                projected,
                projected.Count,
                (int)sw.ElapsedMilliseconds
            )
        );
    }

    private async Task<IReadOnlyList<DataViewEntityRecord>> LoadReportRecordsAsync(
        Report report,
        int maxRows,
        CancellationToken ct
    )
    {
        if (report.DataViewId is { } dvId)
        {
            var dataView = await db
                .DataViews.AsNoTracking()
                .Include(static d => d.Filters)
                .FirstOrDefaultAsync(d => d.Id == dvId, ct);
            if (dataView is not null)
            {
                var (matched, _) = await dataViewEngine.EvaluateAsync(dataView, maxRows, ct);
                return matched;
            }
        }

        return await dataViewEngine.LoadCandidateRecordsAsync(report.EntityTypeKey, maxRows, ct);
    }

    private static List<IReadOnlyDictionary<string, string?>> ProjectAndSortRows(
        IReadOnlyList<DataViewEntityRecord> records,
        List<ReportField> orderedFields,
        Dictionary<string, IFieldSelectPlugin> pluginMap
    )
    {
        List<Dictionary<string, string?>> rows = [];
        foreach (var record in records)
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in orderedFields)
            {
                var raw = pluginMap.TryGetValue(field.FieldSelectKey, out var plugin)
                    ? plugin.SelectValue(record, field.SelectionConfigJson)
                    : null;
                row[field.ColumnHeaderText] = raw is null
                    ? null
                    : DataViewScalarComparer.FormatScalar(raw);
            }

            rows.Add(row);
        }

        var sortFields = orderedFields
            .Where(static f =>
                f.SortOrder is not null && f.SortDirection != ReportSortDirection.None
            )
            .OrderBy(static f => f.SortOrder)
            .ToList();
        if (sortFields.Count > 0)
        {
            rows = ApplySorting(rows, sortFields);
        }

        return [.. rows.Cast<IReadOnlyDictionary<string, string?>>()];
    }

    private static List<Dictionary<string, string?>> ApplySorting(
        List<Dictionary<string, string?>> rows,
        List<ReportField> sortFields
    )
    {
        IOrderedEnumerable<Dictionary<string, string?>>? ordered = null;
        foreach (var sf in sortFields)
        {
            var col = sf.ColumnHeaderText;
            var desc = sf.SortDirection == ReportSortDirection.Descending;
            ordered = ordered is null
                ? desc
                    ? rows.OrderByDescending(
                        r => r.GetValueOrDefault(col),
                        StringComparer.OrdinalIgnoreCase
                    )
                    : rows.OrderBy(r => r.GetValueOrDefault(col), StringComparer.OrdinalIgnoreCase)
                : desc
                    ? ordered.ThenByDescending(
                        r => r.GetValueOrDefault(col),
                        StringComparer.OrdinalIgnoreCase
                    )
                    : ordered.ThenBy(
                        r => r.GetValueOrDefault(col),
                        StringComparer.OrdinalIgnoreCase
                    );
        }

        return ordered is null ? rows : [.. ordered];
    }
}
