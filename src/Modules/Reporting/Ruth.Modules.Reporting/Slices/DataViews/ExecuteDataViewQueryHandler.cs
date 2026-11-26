using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.DataViews;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.Modules.Reporting.Infrastructure.DataViews;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Reporting.Slices.DataViews;

internal sealed class ExecuteDataViewQueryHandler(
    ReportingDbContext db,
    ReportingDataViewExecutionEngine engine,
    IClock clock
) : IQueryHandler<ExecuteDataViewQuery, DataViewExecutionResponse>
{
    public async Task<Result<DataViewExecutionResponse>> HandleAsync(
        ExecuteDataViewQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = DataViewId.From(query.DataViewId);
        var dataView = await db
            .DataViews.AsNoTracking()
            .Include(static d => d.Filters)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (dataView is null)
        {
            return Result.Failure<DataViewExecutionResponse>(
                ReportingErrors.NotFound("DataView.NotFound", "DataView was not found.")
            );
        }

        var maxRows = Math.Clamp(query.MaxRows, 1, 5000);
        if (query.UsePersistedIfFresh && dataView.IsPersistedFreshAt(clock.UtcNow))
        {
            return Result.Success(
                await ExecutePersistedAsync(dataView, maxRows, cancellationToken)
            );
        }

        var sw = Stopwatch.StartNew();
        var (matched, sqlPreview) = await engine.EvaluateAsync(
            dataView,
            maxRows,
            cancellationToken
        );
        sw.Stop();
        List<Guid> ids = [.. matched.Select(static r => r.EntityId)];

        return Result.Success(
            new DataViewExecutionResponse(
                dataView.Id.Value,
                dataView.EntityTypeKey,
                ids.Count,
                (int)sw.ElapsedMilliseconds,
                UsedPersistedValues: false,
                sqlPreview,
                ids,
                matched
            )
        );
    }

    private async Task<DataViewExecutionResponse> ExecutePersistedAsync(
        DataView dataView,
        int maxRows,
        CancellationToken ct
    )
    {
        var sw = Stopwatch.StartNew();
        var persistedIds = await db
            .DataViewPersistedValues.AsNoTracking()
            .Where(v => v.DataViewId == dataView.Id)
            .Select(static v => v.EntityId)
            .Take(maxRows)
            .ToListAsync(ct);
        var candidates = await engine.LoadCandidateRecordsAsync(
            dataView.EntityTypeKey,
            maxRows * 2,
            ct
        );
        var idSet = persistedIds.ToHashSet();
        List<DataViewEntityRecord> records = [.. candidates.Where(r => idSet.Contains(r.EntityId))];
        sw.Stop();

        return new DataViewExecutionResponse(
            dataView.Id.Value,
            dataView.EntityTypeKey,
            persistedIds.Count,
            (int)sw.ElapsedMilliseconds,
            UsedPersistedValues: true,
            "SELECT entity_id FROM reporting.data_view_persisted_values WHERE data_view_id = @p0",
            persistedIds,
            records
        );
    }
}
