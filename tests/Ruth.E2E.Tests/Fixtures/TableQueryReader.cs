using System.Globalization;
using Npgsql;

namespace Ruth.E2E.Tests.Fixtures;

internal static class TableQueryReader
{
    public static async Task<long> CountRowsAsync(
        string connectionString,
        string schemaName,
        string tableName,
        CancellationToken ct
    )
    {
        await using var connection = await OpenConnectionAsync(connectionString, ct);
        await using var command = ProbeSqlCatalog.TryCreateCountCommand(
            connection,
            schemaName,
            tableName
        )
            ?? throw new NotSupportedException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Table '{0}.{1}' is not registered in ProbeSqlCatalog.",
                    schemaName,
                    tableName
                )
            );

        return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    public static async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadRowsAsync(
        string connectionString,
        string schemaName,
        string tableName,
        int limit,
        CancellationToken ct
    )
    {
        await using var connection = await OpenConnectionAsync(connectionString, ct);
        await using var command = ProbeSqlCatalog.TryCreateSelectCommand(
            connection,
            schemaName,
            tableName
        )
            ?? throw new NotSupportedException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Table '{0}.{1}' is not registered in ProbeSqlCatalog.",
                    schemaName,
                    tableName
                )
            );

        _ = command.Parameters.AddWithValue("@limit", limit);

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, ct)
                    ? null
                    : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    public static async Task<long> CountFirstExistingAsync(
        string connectionString,
        string schemaName,
        IReadOnlyList<string> candidates,
        CancellationToken ct
    )
    {
        var tableName = await ResolveRequiredTableAsync(
            connectionString,
            schemaName,
            candidates,
            ct
        );
        return await CountRowsAsync(connectionString, schemaName, tableName, ct);
    }

    public static async Task<
        IReadOnlyList<IReadOnlyDictionary<string, object?>>
    > ReadFirstExistingAsync(
        string connectionString,
        string schemaName,
        IReadOnlyList<string> candidates,
        int limit,
        CancellationToken ct
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        var tableName = await ResolveRequiredTableAsync(
            connectionString,
            schemaName,
            candidates,
            ct
        );
        return await ReadRowsAsync(connectionString, schemaName, tableName, limit, ct);
    }

    private static async Task<string> ResolveRequiredTableAsync(
        string connectionString,
        string schemaName,
        IReadOnlyList<string> candidates,
        CancellationToken ct
    )
    {
        await using var connection = await OpenConnectionAsync(connectionString, ct);
        var tables = await SchemaMetadataReader.GetTablesInSchemaAsync(connection, schemaName, ct);
        return candidates.FirstOrDefault(c => tables.Contains(c, StringComparer.Ordinal))
            ?? throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "None of [{0}] exist in schema '{1}'.",
                    string.Join(", ", candidates),
                    schemaName
                )
            );
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync(
        string connectionString,
        CancellationToken ct
    )
    {
        var connection = new NpgsqlConnection(connectionString);
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
}
