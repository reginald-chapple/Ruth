using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Ruth.Persistence.Scheduling;

/// <summary>
/// Provides session-scoped PostgreSQL advisory locks (pg_try_advisory_lock) over a dedicated
/// <see cref="NpgsqlDataSource" /> with instance-scoped <see cref="AsyncLocal{T}" /> reentrancy protection.
/// </summary>
public sealed class PostgresAdvisoryLockProvider : IAsyncDisposable, IDisposable
{
    private readonly NpgsqlDataSource _lockDataSource;
    private readonly ILogger<PostgresAdvisoryLockProvider> _logger;
    private readonly AsyncLocal<List<ActiveLockSlot>?> _activeLocks = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _localSemaphores = new(
        StringComparer.Ordinal
    );

    /// <summary>Initializes a new <see cref="PostgresAdvisoryLockProvider" />.</summary>
    public PostgresAdvisoryLockProvider(
        string connectionString,
        IOptions<SchedulerOptions> options,
        ILogger<PostgresAdvisoryLockProvider> logger
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = "RuthSchedulerLock",
            MaxPoolSize = Math.Max(5, options.Value.LockMaxPoolSize),
        };

        _lockDataSource = NpgsqlDataSource.Create(builder.ConnectionString);
    }

    /// <summary>Computes a deterministic 64-bit PostgreSQL advisory lock key for <paramref name="lockKey" />.</summary>
    public static long ComputeAdvisoryLockKey(string lockKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockKey);

        Span<byte> hash = stackalloc byte[32];
        var input = Encoding.UTF8.GetBytes($"ruth:scheduler:job:{lockKey}");
        _ = SHA256.HashData(input, hash);
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }

    /// <summary>
    /// Attempts to acquire a non-blocking distributed lock for <paramref name="lockKey" />,
    /// returning <see langword="null" /> if another worker already holds it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the current async flow already holds <paramref name="lockKey" />.</exception>
    public Task<IAsyncDisposable?> TryAcquireAsync(
        string lockKey,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockKey);

        var currentSlots = _activeLocks.Value;
        if (currentSlots is not null && HasActiveSlot(currentSlots, lockKey))
        {
            throw new InvalidOperationException(
                $"Reentrant acquisition of advisory lock '{lockKey}' on the same async flow is not allowed."
            );
        }

        var semaphore = _localSemaphores.GetOrAdd(lockKey, static _ => new SemaphoreSlim(1, 1));
        if (!semaphore.Wait(0, cancellationToken))
        {
            return Task.FromResult<IAsyncDisposable?>(null);
        }

        var slot = new ActiveLockSlot(lockKey);
        var nextSlots = new List<ActiveLockSlot>();
        if (currentSlots is not null)
        {
            foreach (var existing in currentSlots)
            {
                if (existing.IsAcquired)
                {
                    nextSlots.Add(existing);
                }
            }
        }

        nextSlots.Add(slot);
        _activeLocks.Value = nextSlots;

        return AcquirePostgresLockAsync(lockKey, semaphore, slot, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        DisposeSemaphores();
        return _lockDataSource.DisposeAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeSemaphores();
        _lockDataSource.Dispose();
    }

    private async Task<IAsyncDisposable?> AcquirePostgresLockAsync(
        string lockKey,
        SemaphoreSlim semaphore,
        ActiveLockSlot slot,
        CancellationToken cancellationToken
    )
    {
        var advisoryKey = ComputeAdvisoryLockKey(lockKey);
        NpgsqlConnection? connection = null;

        try
        {
            connection = await _lockDataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_try_advisory_lock(@key);";
            _ = command.Parameters.Add(
                new NpgsqlParameter("key", NpgsqlDbType.Bigint) { Value = advisoryKey }
            );

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var acquired = result is true;
            if (!acquired)
            {
                slot.IsAcquired = false;
                await connection.DisposeAsync();
                _ = semaphore.Release();
                return null;
            }

            slot.IsAcquired = true;
            return new PostgresAdvisoryLockHandle(
                lockKey,
                advisoryKey,
                connection,
                semaphore,
                () => slot.IsAcquired = false,
                (ex, key) => SchedulerLog.UnlockFailed(_logger, ex, key)
            );
        }
        catch
        {
            slot.IsAcquired = false;
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }

            _ = semaphore.Release();
            throw;
        }
    }

    private static bool HasActiveSlot(List<ActiveLockSlot> slots, string lockKey)
    {
        foreach (var slot in slots)
        {
            if (slot.IsAcquired && string.Equals(slot.LockKey, lockKey, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void DisposeSemaphores()
    {
        foreach (var semaphore in _localSemaphores.Values)
        {
            semaphore.Dispose();
        }

        _localSemaphores.Clear();
    }

    private sealed class ActiveLockSlot(string lockKey)
    {
        public string LockKey { get; } = lockKey;
        public bool IsAcquired { get; set; }
    }
}
