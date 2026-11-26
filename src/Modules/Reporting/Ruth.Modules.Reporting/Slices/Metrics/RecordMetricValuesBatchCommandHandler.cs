using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.Metrics;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Metrics;

internal sealed class RecordMetricValuesBatchCommandHandler(ReportingDbContext db)
    : ICommandHandler<RecordMetricValuesBatchCommand, IReadOnlyList<MetricValueResponse>>
{
    public async Task<Result<IReadOnlyList<MetricValueResponse>>> HandleAsync(
        RecordMetricValuesBatchCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (command.Entries is not { Count: > 0 })
        {
            return Result.Failure<IReadOnlyList<MetricValueResponse>>(
                ReportingErrors.Validation(
                    "MetricValue.EntriesRequired",
                    "At least one metric value entry is required."
                )
            );
        }

        List<MetricId> metricIds =
        [
            .. command.Entries.Select(static e => MetricId.From(e.MetricId)).Distinct(),
        ];
        var metrics = await db
            .Metrics.AsNoTracking()
            .Include(static m => m.Partitions)
            .Where(m => metricIds.Contains(m.Id))
            .ToDictionaryAsync(static m => m.Id, cancellationToken);

        List<MetricValue> created = [];
        foreach (var input in command.Entries)
        {
            var id = MetricId.From(input.MetricId);
            if (!metrics.TryGetValue(id, out var metric))
            {
                return Result.Failure<IReadOnlyList<MetricValueResponse>>(
                    ReportingErrors.NotFound(
                        "Metric.NotFound",
                        $"Metric '{input.MetricId:D}' was not found."
                    )
                );
            }

            var valRes = MetricValue.Create(
                metric,
                input.MetricValueType,
                input.YValue,
                input.MetricValueDateTimeUtc,
                input.XValue,
                input.Note,
                input.Partitions
            );
            if (valRes.IsFailure)
            {
                return Result.Failure<IReadOnlyList<MetricValueResponse>>(valRes.PrimaryError);
            }

            created.Add(valRes.Value);
            _ = db.MetricValues.Add(valRes.Value);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<MetricValueResponse>>([
            .. created.Select(ReportingDtoMapper.ToResponse),
        ]);
    }
}
