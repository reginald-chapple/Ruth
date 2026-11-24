using Npgsql;
using NpgsqlTypes;

namespace Ruth.Persistence.Scheduling;

internal sealed class PostgresAdvisoryLockHandle(
    string lockKey,
    long advisoryKey,
    NpgsqlConnection connection,
    SemaphoreSlim localSemaphore,
    Action onDisposed,
    Action<Exception, string> onUnlockFailed
) : IAsyncDisposable
{
    private readonly string _lockKey = lockKey;
    private readonly long _advisoryKey = advisoryKey;
    private readonly NpgsqlConnection _connection = connection;
    private readonly SemaphoreSlim _localSemaphore = localSemaphore;
    private readonly Action _onDisposed = onDisposed;
    private readonly Action<Exception, string> _onUnlockFailed = onUnlockFailed;
    private int _disposed;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _onDisposed();

        try
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = "SELECT pg_advisory_unlock(@key);";
            command.CommandTimeout = 5;
            _ = command.Parameters.Add(
                new NpgsqlParameter("key", NpgsqlDbType.Bigint) { Value = _advisoryKey }
            );
            _ = await command.ExecuteScalarAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _onUnlockFailed(ex, _lockKey);
        }
        finally
        {
            await _connection.DisposeAsync();
            _ = _localSemaphore.Release();
        }
    }
}
