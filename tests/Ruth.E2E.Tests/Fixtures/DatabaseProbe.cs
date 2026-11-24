using Npgsql;

namespace Ruth.E2E.Tests.Fixtures;

public sealed class DatabaseProbe
{
    public DatabaseProbe(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ConnectionString = connectionString;
    }

    internal string ConnectionString { get; }

    public async Task<IReadOnlyList<string>> GetInstalledExtensionsAsync(
        CancellationToken ct = default
    )
    {
        await using var connection = await OpenConnectionAsync(ct);
        return await SchemaMetadataReader.GetInstalledExtensionsAsync(connection, ct);
    }

    public async Task<bool> SchemaExistsAsync(string schemaName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        await using var connection = await OpenConnectionAsync(ct);
        return await SchemaMetadataReader.SchemaExistsAsync(connection, schemaName, ct);
    }

    public async Task<bool> TableExistsAsync(
        string schemaName,
        string tableName,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        await using var connection = await OpenConnectionAsync(ct);
        return await SchemaMetadataReader.TableExistsAsync(connection, schemaName, tableName, ct);
    }

    public async Task<IReadOnlyList<string>> GetTablesInSchemaAsync(
        string schemaName,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        await using var connection = await OpenConnectionAsync(ct);
        return await SchemaMetadataReader.GetTablesInSchemaAsync(connection, schemaName, ct);
    }

    public async Task<string?> ResolveFirstExistingTableAsync(
        string schemaName,
        IReadOnlyList<string> candidateTableNames,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(candidateTableNames);
        var tables = await GetTablesInSchemaAsync(schemaName, ct);
        return candidateTableNames.FirstOrDefault(c => tables.Contains(c, StringComparer.Ordinal));
    }

    public async Task<IReadOnlyList<ColumnSchemaInfo>> GetColumnsAsync(
        string schemaName,
        string tableName,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        await using var connection = await OpenConnectionAsync(ct);
        return await SchemaMetadataReader.GetColumnsAsync(connection, schemaName, tableName, ct);
    }

    public async Task<IReadOnlyList<string>> GetPrimaryKeyColumnsAsync(
        string schemaName,
        string tableName,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        await using var connection = await OpenConnectionAsync(ct);
        return await SchemaMetadataReader.GetPrimaryKeyColumnsAsync(
            connection,
            schemaName,
            tableName,
            ct
        );
    }

    public async Task<IReadOnlyList<IndexSchemaInfo>> GetIndexesAsync(
        string schemaName,
        string tableName,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        await using var connection = await OpenConnectionAsync(ct);
        return await SchemaMetadataReader.GetIndexesAsync(connection, schemaName, tableName, ct);
    }

    public Task<long> CountRowsAsync(
        string schemaName,
        string tableName,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        return TableQueryReader.CountRowsAsync(ConnectionString, schemaName, tableName, ct);
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadRowsAsync(
        string schemaName,
        string tableName,
        int limit = 100,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        return TableQueryReader.ReadRowsAsync(ConnectionString, schemaName, tableName, limit, ct);
    }

    public async Task<AdvisoryLockLease> TryAcquireAdvisoryLockAsync(
        long lockKey,
        CancellationToken ct = default
    )
    {
        var connection = await OpenConnectionAsync(ct);
        try
        {
            const string lockSql = "SELECT pg_try_advisory_lock(@lockKey);";
            await using var command = new NpgsqlCommand(lockSql, connection);
            _ = command.Parameters.AddWithValue("@lockKey", lockKey);
            var acquired = (bool)(await command.ExecuteScalarAsync(ct) ?? false);
            return new AdvisoryLockLease(connection, lockKey, acquired);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public static bool TryGetColumnValue(
        IReadOnlyDictionary<string, object?> row,
        string candidateColumnName,
        out object? value
    )
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateColumnName);

        if (row.TryGetValue(candidateColumnName, out value))
        {
            return true;
        }

        var normalizedCandidate = NormalizeIdentifier(candidateColumnName);
        foreach (var kvp in row)
        {
            var normalizedKey = NormalizeIdentifier(kvp.Key);
            if (string.Equals(normalizedKey, normalizedCandidate, StringComparison.Ordinal))
            {
                value = kvp.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var connection = new NpgsqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static string NormalizeIdentifier(string identifier)
    {
        return identifier.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }
}
