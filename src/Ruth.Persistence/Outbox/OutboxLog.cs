using Microsoft.Extensions.Logging;

namespace Ruth.Persistence.Outbox;

internal static partial class OutboxLog
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Debug,
        Message = "Claimed {Count} Outbox message(s) for schema {Schema}."
    )]
    public static partial void ClaimedBatch(ILogger logger, int count, string schema);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "Outbox message {MessageId} in schema {Schema} failed on attempt {RetryCount}; scheduled retry at {NextRetryOnUtc}."
    )]
    public static partial void ScheduledRetry(
        ILogger logger,
        Exception exception,
        Guid messageId,
        string schema,
        int retryCount,
        DateTimeOffset nextRetryOnUtc
    );

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Error,
        Message = "Outbox message {MessageId} in schema {Schema} was dead-lettered: {Reason}"
    )]
    public static partial void DeadLettered(
        ILogger logger,
        Exception? exception,
        Guid messageId,
        string schema,
        string reason
    );

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Debug,
        Message = "Consumer {ConsumerName} skipped already-processed integration event {MessageId}."
    )]
    public static partial void DuplicateSkipped(
        ILogger logger,
        string consumerName,
        Guid messageId
    );

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Error,
        Message = "Outbox dispatcher sweep failed unexpectedly."
    )]
    public static partial void SweepFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Error,
        Message = "Outbox batch processing failed for schema {Schema}."
    )]
    public static partial void SchemaBatchFailed(
        ILogger logger,
        Exception exception,
        string schema
    );

    [LoggerMessage(
        EventId = 3007,
        Level = LogLevel.Warning,
        Message = "Outbox message {MessageId} in schema {Schema} lost lease ownership before {Operation} could commit."
    )]
    public static partial void LeaseLost(
        ILogger logger,
        Guid messageId,
        string schema,
        string operation
    );

    [LoggerMessage(
        EventId = 3008,
        Level = LogLevel.Information,
        Message = "Pruned {DeletedCount} processed Outbox message(s) in schema {Schema} older than {CutoffUtc}."
    )]
    public static partial void PrunedProcessed(
        ILogger logger,
        int deletedCount,
        string schema,
        DateTimeOffset cutoffUtc
    );
}
