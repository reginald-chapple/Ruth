using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.Outbox;

internal sealed class OutboxStatusUpdater<TContext>(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    OutboxOptions options,
    ILogger logger,
    string schema
)
    where TContext : ModuleDbContext
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IClock _clock = clock;
    private readonly OutboxOptions _options = options;
    private readonly ILogger _logger = logger;
    private readonly string _schema = schema;

    public async Task MarkProcessedAsync(
        Guid messageId,
        Guid lockToken,
        CancellationToken cancellationToken
    )
    {
        var completedAt = _clock.UtcNow;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var updated = await dbContext
            .OutboxMessages.Where(x =>
                x.Id == messageId
                && x.LockToken == lockToken
                && x.ProcessedOnUtc == null
                && x.DeadLetteredOnUtc == null
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(x => x.ProcessedOnUtc, completedAt)
                        .SetProperty(x => x.LockedUntilUtc, (DateTimeOffset?)null)
                        .SetProperty(x => x.LockToken, (Guid?)null)
                        .SetProperty(x => x.Error, (string?)null),
                cancellationToken
            );

        if (updated == 0)
        {
            OutboxLog.LeaseLost(_logger, messageId, _schema, nameof(MarkProcessedAsync));
        }
    }

    public async Task HandleFailureAsync(
        OutboxClaimedMessage message,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var nextRetryCount = message.RetryCount + 1;
        var error = TruncateError($"{exception.GetType().Name}: {exception.Message}");

        if (OutboxRetryPolicy.ShouldDeadLetter(nextRetryCount, _options.MaxRetries))
        {
            await DeadLetterAsync(
                message.Id,
                message.LockToken,
                nextRetryCount,
                error,
                exception,
                cancellationToken
            );
            return;
        }

        await ScheduleRetryAsync(message, nextRetryCount, error, exception, cancellationToken);
    }

    private async Task ScheduleRetryAsync(
        OutboxClaimedMessage message,
        int nextRetryCount,
        string error,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var delay = OutboxRetryPolicy.ComputeBackoffDelay(
            nextRetryCount,
            _options.BaseRetryDelay,
            _options.MaxRetryDelay
        );
        var nextRetryOnUtc = _clock.UtcNow + delay;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var updated = await dbContext
            .OutboxMessages.Where(x =>
                x.Id == message.Id
                && x.LockToken == message.LockToken
                && x.ProcessedOnUtc == null
                && x.DeadLetteredOnUtc == null
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(x => x.RetryCount, nextRetryCount)
                        .SetProperty(x => x.NextRetryOnUtc, nextRetryOnUtc)
                        .SetProperty(x => x.LockedUntilUtc, (DateTimeOffset?)null)
                        .SetProperty(x => x.LockToken, (Guid?)null)
                        .SetProperty(x => x.Error, error),
                cancellationToken
            );

        if (updated == 0)
        {
            OutboxLog.LeaseLost(_logger, message.Id, _schema, nameof(HandleFailureAsync));
            return;
        }

        OutboxLog.ScheduledRetry(
            _logger,
            exception,
            message.Id,
            _schema,
            nextRetryCount,
            nextRetryOnUtc
        );
    }

    public async Task DeadLetterAsync(
        Guid messageId,
        Guid lockToken,
        int retryCount,
        string reason,
        Exception? exception,
        CancellationToken cancellationToken
    )
    {
        var deadLetteredAt = _clock.UtcNow;
        var boundedReason = TruncateError(reason);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var updated = await dbContext
            .OutboxMessages.Where(x =>
                x.Id == messageId
                && x.LockToken == lockToken
                && x.ProcessedOnUtc == null
                && x.DeadLetteredOnUtc == null
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(x => x.RetryCount, retryCount)
                        .SetProperty(x => x.DeadLetteredOnUtc, deadLetteredAt)
                        .SetProperty(x => x.LockedUntilUtc, (DateTimeOffset?)null)
                        .SetProperty(x => x.LockToken, (Guid?)null)
                        .SetProperty(x => x.Error, boundedReason),
                cancellationToken
            );

        if (updated == 0)
        {
            OutboxLog.LeaseLost(_logger, messageId, _schema, nameof(DeadLetterAsync));
            return;
        }

        OutboxLog.DeadLettered(_logger, exception, messageId, _schema, boundedReason);
    }

    public static string TruncateError(string message)
    {
        const int maxLength = 2048;
        return message.Length <= maxLength ? message : message[..maxLength];
    }
}
