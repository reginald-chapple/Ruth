namespace Ruth.Persistence.Scheduling;

/// <summary>Configuration options for the PostgreSQL advisory-locked <see cref="ScheduledJobEngine" />.</summary>
public sealed class SchedulerOptions
{
    /// <summary>Gets or sets the timer interval for polling due jobs.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Gets or sets the max pool size for the dedicated advisory-lock NpgsqlDataSource.</summary>
    public int LockMaxPoolSize { get; set; } = 50;

    /// <summary>Gets or sets the default retained history count when a job specifies &lt;= 0.</summary>
    public int DefaultHistoryCount { get; set; } = 500;

    /// <summary>Gets or sets the worker node identifier recorded on job runs.</summary>
    public string WorkerId { get; set; } = Environment.MachineName;
}
