using Microsoft.Extensions.Logging;

namespace Ruth.Persistence.Scheduling;

internal static partial class SchedulerLog
{
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Debug,
        Message = "Skipped scheduled job {JobKey} because its advisory lock is held by another worker."
    )]
    public static partial void LockContentionSkipped(ILogger logger, string jobKey);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Warning,
        Message = "Failed to release PostgreSQL advisory lock for job {JobKey}."
    )]
    public static partial void UnlockFailed(ILogger logger, Exception exception, string jobKey);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Warning,
        Message = "Scheduled job {JobKey} completed with warning: {Message}"
    )]
    public static partial void JobWarning(ILogger logger, string jobKey, string? message);

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Error,
        Message = "Scheduled job {JobKey} failed: {Message}"
    )]
    public static partial void JobFailed(
        ILogger logger,
        Exception? exception,
        string jobKey,
        string? message
    );

    [LoggerMessage(
        EventId = 4005,
        Level = LogLevel.Error,
        Message = "Failed to persist scheduled job bookkeeping for {JobKey} ({Phase})."
    )]
    public static partial void BookkeepingFailed(
        ILogger logger,
        Exception exception,
        string jobKey,
        string phase
    );

    [LoggerMessage(
        EventId = 4006,
        Level = LogLevel.Error,
        Message = "Scheduled job engine polling sweep failed unexpectedly."
    )]
    public static partial void EngineSweepFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 4007,
        Level = LogLevel.Warning,
        Message = "Scheduled job {JobKey} has an invalid cron expression '{CronExpression}'."
    )]
    public static partial void InvalidCronExpression(
        ILogger logger,
        string jobKey,
        string cronExpression
    );

    [LoggerMessage(
        EventId = 4008,
        Level = LogLevel.Warning,
        Message = "Recovered orphaned 'Running' state for scheduled job {JobKey}."
    )]
    public static partial void RecoveredOrphanedJob(ILogger logger, string jobKey);

    [LoggerMessage(
        EventId = 4009,
        Level = LogLevel.Debug,
        Message = "Skipped scheduled job {JobKey} because it is no longer active or due after acquiring the advisory lock."
    )]
    public static partial void NotDueOrInactiveSkipped(ILogger logger, string jobKey);

    [LoggerMessage(
        EventId = 4010,
        Level = LogLevel.Warning,
        Message = "Scheduled job engine initialization failed; will retry on next polling tick."
    )]
    public static partial void InitializationFailed(ILogger logger, Exception exception);
}
