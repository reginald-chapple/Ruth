using StackExchange.Redis;

namespace Ruth.Host.Health;

internal sealed class RedisConnectionAccessor(IConfiguration configuration) : IAsyncDisposable
{
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private ConnectionMultiplexer? _multiplexer;
    private string? _configuredConnectionString;
    private bool _disposed;

    public async Task<TimeSpan> PingAsync(int timeoutSeconds, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var connectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Redis connection string is not configured.");
        }

        var multiplexer = await GetOrCreateMultiplexerAsync(
            connectionString,
            timeoutSeconds,
            cancellationToken
        );

        var database = multiplexer.GetDatabase();
        return await database.PingAsync().WaitAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_multiplexer is not null)
        {
            await _multiplexer.DisposeAsync();
            _multiplexer = null;
        }

        _connectLock.Dispose();
    }

    private async Task<ConnectionMultiplexer> GetOrCreateMultiplexerAsync(
        string connectionString,
        int timeoutSeconds,
        CancellationToken cancellationToken
    )
    {
        if (
            _multiplexer is not null
            && string.Equals(
                _configuredConnectionString,
                connectionString,
                StringComparison.Ordinal
            )
        )
        {
            return _multiplexer;
        }

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (
                _multiplexer is not null
                && string.Equals(
                    _configuredConnectionString,
                    connectionString,
                    StringComparison.Ordinal
                )
            )
            {
                return _multiplexer;
            }

            if (_multiplexer is not null)
            {
                await _multiplexer.DisposeAsync();
                _multiplexer = null;
            }

            var timeoutMs = Math.Clamp(timeoutSeconds * 1000, 500, 5000);
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = Math.Min(options.ConnectTimeout, timeoutMs);
            options.SyncTimeout = Math.Min(options.SyncTimeout, timeoutMs);
            options.AsyncTimeout = Math.Min(options.AsyncTimeout, timeoutMs);
            options.ConnectRetry = 1;

            _multiplexer = await ConnectWithCancellationCleanupAsync(options, cancellationToken);
            _configuredConnectionString = connectionString;
            return _multiplexer;
        }
        finally
        {
            _ = _connectLock.Release();
        }
    }

    private static async Task<ConnectionMultiplexer> ConnectWithCancellationCleanupAsync(
        ConfigurationOptions options,
        CancellationToken cancellationToken
    )
    {
        var connectTask = ConnectionMultiplexer.ConnectAsync(options);
        try
        {
            return await connectTask.WaitAsync(cancellationToken);
        }
        catch
        {
            _ = connectTask.ContinueWith(
                static task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        task.Result.Dispose();
                    }
                    else
                    {
                        _ = task.Exception;
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
            throw;
        }
    }
}
