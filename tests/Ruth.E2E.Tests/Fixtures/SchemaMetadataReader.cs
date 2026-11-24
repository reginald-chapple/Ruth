using Npgsql;

namespace Ruth.E2E.Tests.Fixtures;

internal static class SchemaMetadataReader
{
    public static async Task<IReadOnlyList<string>> GetInstalledExtensionsAsync(
        NpgsqlConnection connection,
        CancellationToken ct
    )
    {
        const string sql = "SELECT extname FROM pg_extension ORDER BY extname;";
        await using var command = new NpgsqlCommand(sql, connection);
        return await ProbeSqlCatalog.ReadStringListAsync(command, ct);
    }

    public static async Task<bool> SchemaExistsAsync(
        NpgsqlConnection connection,
        string schemaName,
        CancellationToken ct
    )
    {
        const string sql =
            "SELECT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = @schemaName);";

        await using var command = new NpgsqlCommand(sql, connection);
        _ = command.Parameters.AddWithValue("@schemaName", schemaName);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    public static async Task<bool> TableExistsAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken ct
    )
    {
        const string sql =
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables"
            + " WHERE table_schema = @schemaName"
            + " AND table_name = @tableName"
            + " AND table_type = 'BASE TABLE');";

        await using var command = new NpgsqlCommand(sql, connection);
        _ = command.Parameters.AddWithValue("@schemaName", schemaName);
        _ = command.Parameters.AddWithValue("@tableName", tableName);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    public static async Task<IReadOnlyList<string>> GetTablesInSchemaAsync(
        NpgsqlConnection connection,
        string schemaName,
        CancellationToken ct
    )
    {
        const string sql =
            "SELECT table_name FROM information_schema.tables"
            + " WHERE table_schema = @schemaName AND table_type = 'BASE TABLE'"
            + " ORDER BY table_name;";

        await using var command = new NpgsqlCommand(sql, connection);
        _ = command.Parameters.AddWithValue("@schemaName", schemaName);
        return await ProbeSqlCatalog.ReadStringListAsync(command, ct);
    }

    public static async Task<IReadOnlyList<ColumnSchemaInfo>> GetColumnsAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken ct
    )
    {
        const string sql =
            "SELECT column_name, data_type, udt_name, is_nullable"
            + " FROM information_schema.columns"
            + " WHERE table_schema = @schemaName AND table_name = @tableName"
            + " ORDER BY ordinal_position;";

        await using var command = new NpgsqlCommand(sql, connection);
        _ = command.Parameters.AddWithValue("@schemaName", schemaName);
        _ = command.Parameters.AddWithValue("@tableName", tableName);

        var columns = new List<ColumnSchemaInfo>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var isNullable = string.Equals(
                reader.GetString(3),
                "YES",
                StringComparison.OrdinalIgnoreCase
            );
            columns.Add(
                new ColumnSchemaInfo(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    isNullable
                )
            );
        }

        return columns;
    }

    public static async Task<IReadOnlyList<string>> GetPrimaryKeyColumnsAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken ct
    )
    {
        const string sql =
            "SELECT kcu.column_name"
            + " FROM information_schema.table_constraints tc"
            + " JOIN information_schema.key_column_usage kcu"
            + " ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema"
            + " WHERE tc.constraint_type = 'PRIMARY KEY'"
            + " AND tc.table_schema = @schemaName AND tc.table_name = @tableName"
            + " ORDER BY kcu.ordinal_position;";

        await using var command = new NpgsqlCommand(sql, connection);
        _ = command.Parameters.AddWithValue("@schemaName", schemaName);
        _ = command.Parameters.AddWithValue("@tableName", tableName);
        return await ProbeSqlCatalog.ReadStringListAsync(command, ct);
    }

    public static async Task<IReadOnlyList<IndexSchemaInfo>> GetIndexesAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken ct
    )
    {
        const string sql =
            "SELECT indexname, indexdef FROM pg_indexes"
            + " WHERE schemaname = @schemaName AND tablename = @tableName"
            + " ORDER BY indexname;";

        await using var command = new NpgsqlCommand(sql, connection);
        _ = command.Parameters.AddWithValue("@schemaName", schemaName);
        _ = command.Parameters.AddWithValue("@tableName", tableName);

        var indexes = new List<IndexSchemaInfo>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            indexes.Add(new IndexSchemaInfo(reader.GetString(0), reader.GetString(1)));
        }

        return indexes;
    }
}
