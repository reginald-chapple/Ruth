using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Ruth.Persistence.Scheduling;

/// <summary>OpenTelemetry instrumentation primitives for <see cref="ScheduledJobEngine" />.</summary>
public static class SchedulerDiagnostics
{
    /// <summary>ActivitySource name for scheduled job root spans.</summary>
    public const string ActivitySourceName = "Ruth.Persistence.Scheduler";

    /// <summary>Meter name for scheduled job counters.</summary>
    public const string MeterName = "Ruth.Persistence.Scheduler";

    /// <summary>Shared <see cref="System.Diagnostics.ActivitySource" /> for scheduled job execution.</summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>Shared <see cref="System.Diagnostics.Metrics.Meter" /> for scheduled job metrics.</summary>
    public static readonly Meter Meter = new(MeterName);

    /// <summary>Counter tracking executed scheduled jobs.</summary>
    public static readonly Counter<long> JobsExecuted = Meter.CreateCounter<long>(
        "ruth.scheduler.jobs.executed",
        description: "Number of scheduled job executions completed."
    );

    /// <summary>Counter tracking failed scheduled jobs.</summary>
    public static readonly Counter<long> JobsFailed = Meter.CreateCounter<long>(
        "ruth.scheduler.jobs.failed",
        description: "Number of scheduled job executions that failed."
    );

    /// <summary>Counter tracking scheduled job runs skipped due to advisory lock contention.</summary>
    public static readonly Counter<long> JobsSkippedContention = Meter.CreateCounter<long>(
        "ruth.scheduler.jobs.skipped_contention",
        description: "Number of scheduled job executions skipped because another worker held the advisory lock."
    );
}
