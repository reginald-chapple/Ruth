using System.Data;
using Npgsql;

namespace Ruth.E2E.Tests.Fixtures;

public sealed class AdvisoryLockLease : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly long _lockKey;
    private bool _disposed;

    internal AdvisoryLockLease(NpgsqlConnection connection, long lockKey, bool acquired)
    {
        _connection = connection;
        _lockKey = lockKey;
        IsAcquired = acquired;
    }

    public bool IsAcquired { get; }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            if (IsAcquired && _connection.State == ConnectionState.Open)
            {
                const string unlockSql = "SELECT pg_advisory_unlock(@lockKey);";
                await using var command = new NpgsqlCommand(unlockSql, _connection);
                _ = command.Parameters.AddWithValue("@lockKey", _lockKey);
                _ = await command.ExecuteScalarAsync();
            }
        }
        finally
        {
            await _connection.DisposeAsync();
        }
    }
}
