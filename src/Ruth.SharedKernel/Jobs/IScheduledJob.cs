namespace Ruth.SharedKernel.Jobs;

/// <summary>Contract for a recurring background job executed under a PostgreSQL advisory lock.</summary>
public interface IScheduledJob
{
    /// <summary>Gets the stable unique key identifying this job across nodes and schedules.</summary>
    string JobKey { get; }

    /// <summary>Gets the default 5-field (or 6-field) cron expression used to seed the job schedule.</summary>
    string DefaultCronExpression { get; }

    /// <summary>Executes one job run.</summary>
    /// <param name="cancellationToken">A token cancelled on host shutdown or job cancellation.</param>
    Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
