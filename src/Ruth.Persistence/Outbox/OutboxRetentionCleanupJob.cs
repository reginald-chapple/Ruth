using System.Globalization;
using Microsoft.Extensions.Options;
using Ruth.Persistence.Scheduling;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.Outbox;

/// <summary>
/// Periodic maintenance job pruning processed transactional Outbox records past
/// <see cref="OutboxOptions.ProcessedRetentionPeriod" /> across all registered module schemas.
/// </summary>
public sealed class OutboxRetentionCleanupJob(
    IEnumerable<IOutboxModuleProcessor> processors,
    IOptions<OutboxOptions> options,
    IClock clock
) : IScheduledJob, IInitialSyncDueJob, IPlatformScheduledJob
{
    private readonly IOutboxModuleProcessor[] _processors = processors?.ToArray()
        ?? throw new ArgumentNullException(nameof(processors));
    private readonly OutboxOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <inheritdoc />
    public string JobKey => "ruth.persistence.outbox-retention-cleanup";

    /// <inheritdoc />
    public string DefaultCronExpression => "0 * * * *";

    /// <inheritdoc />
    public async Task<JobExecutionResult> ExecuteAsync(
        CancellationToken cancellationToken = default
    )
    {
        var retention = _options.ProcessedRetentionPeriod;
        if (retention < TimeSpan.Zero)
        {
            return JobExecutionResult.Warning(
                "Outbox retention cleanup skipped because ProcessedRetentionPeriod is negative."
            );
        }

        var nowUtc = _clock.UtcNow;
        var maxSubtractable = nowUtc - DateTimeOffset.MinValue;
        var cutoffUtc = retention >= maxSubtractable ? DateTimeOffset.MinValue : nowUtc - retention;
        var totalDeleted = 0;
        var failedSchemas = 0;

        foreach (var processor in _processors)
        {
            try
            {
                totalDeleted += await processor.PruneProcessedAsync(cutoffUtc, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                failedSchemas++;
            }
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"Pruned {totalDeleted} processed Outbox message(s) older than {cutoffUtc:O}."
        );
        return failedSchemas > 0
            ? JobExecutionResult.Warning(summary)
            : JobExecutionResult.Success(summary);
    }
}
