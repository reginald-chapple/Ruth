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

internal sealed class MaterializeDataViewCommandHandler(
    ReportingDbContext db,
    ReportingDataViewExecutionEngine engine,
    IClock clock
) : ICommandHandler<MaterializeDataViewCommand, DataViewExecutionResponse>
{
    public async Task<Result<DataViewExecutionResponse>> HandleAsync(
        MaterializeDataViewCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = DataViewId.From(command.DataViewId);
        var dataView = await db
            .DataViews.Include(static d => d.Filters)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (dataView is null)
        {
            return Result.Failure<DataViewExecutionResponse>(
                ReportingErrors.NotFound("DataView.NotFound", "DataView was not found.")
            );
        }

        var sw = Stopwatch.StartNew();
        var (records, sqlPreview) = await engine.EvaluateAsync(
            dataView,
            maxRows: 5000,
            cancellationToken
        );
        sw.Stop();

        var nowUtc = clock.UtcNow;
        var existingRows = await db
            .DataViewPersistedValues.Where(v => v.DataViewId == id)
            .ToListAsync(cancellationToken);
        db.DataViewPersistedValues.RemoveRange(existingRows);

        List<Guid> entityIds = [.. records.Select(static r => r.EntityId).Distinct()];
        foreach (var entityId in entityIds)
        {
            _ = db.DataViewPersistedValues.Add(new DataViewPersistedValue(id, entityId, nowUtc));
        }

        dataView.RecordMaterialization(entityIds.Count, (int)sw.ElapsedMilliseconds, nowUtc);
        _ = await db.SaveChangesAsync(cancellationToken);

        return Result.Success(
            new DataViewExecutionResponse(
                dataView.Id.Value,
                dataView.EntityTypeKey,
                entityIds.Count,
                (int)sw.ElapsedMilliseconds,
                UsedPersistedValues: false,
                sqlPreview,
                entityIds,
                records
            )
        );
    }
}
