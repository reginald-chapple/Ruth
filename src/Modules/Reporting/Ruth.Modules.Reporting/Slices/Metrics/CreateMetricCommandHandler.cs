using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.Metrics;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Metrics;

internal sealed class CreateMetricCommandHandler(ReportingDbContext db)
    : ICommandHandler<CreateMetricCommand, MetricResponse>
{
    public async Task<Result<MetricResponse>> HandleAsync(
        CreateMetricCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedKey = ReportingTextNormalizer.NormalizeKey(command.MetricKey);
        var dup = await db
            .Metrics.AsNoTracking()
            .AnyAsync(m => m.MetricKey == normalizedKey, cancellationToken);
        if (dup)
        {
            return Result.Failure<MetricResponse>(
                ReportingErrors.Conflict(
                    "Metric.DuplicateKey",
                    $"A metric with key '{normalizedKey}' already exists."
                )
            );
        }

        MetricCategoryId? catId =
            command.CategoryId is { } cid && cid != Guid.Empty ? MetricCategoryId.From(cid) : null;
        DataViewId? dvId =
            command.DataViewId is { } dvid && dvid != Guid.Empty ? DataViewId.From(dvid) : null;

        var created = Metric.Create(
            command.Title,
            command.MetricKey,
            command.SourceType,
            command.Subtitle,
            command.Description,
            catId,
            command.IsCumulative,
            dvId,
            command.SourceQueryKey,
            command.ScheduleCronExpression,
            command.YAxisLabel,
            command.UnitLabel,
            command.Partitions
        );
        if (created.IsFailure)
        {
            return Result.Failure<MetricResponse>(created.PrimaryError);
        }

        _ = db.Metrics.Add(created.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(ReportingDtoMapper.ToResponse(created.Value));
    }
}
