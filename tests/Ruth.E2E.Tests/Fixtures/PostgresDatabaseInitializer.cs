using Npgsql;

namespace Ruth.E2E.Tests.Fixtures;

internal static class PostgresDatabaseInitializer
{
    private static readonly string[] _wave0Extensions =
    [
        "uuid-ossp",
        "postgis",
        "btree_gist",
        "vector",
        "age",
    ];

    public static async Task EnsureExtensionsAsync(string connectionString, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        var available = await GetAvailableExtensionsAsync(connection, ct);
        foreach (var extension in _wave0Extensions)
        {
            if (!available.Contains(extension))
            {
                throw new InvalidOperationException(
                    $"Required PostgreSQL 18 extension '{extension}' is not available in pg_available_extensions."
                );
            }

            var sql = ResolveExtensionSql(extension);
            await using var command = new NpgsqlCommand(sql, connection);
            _ = await command.ExecuteNonQueryAsync(ct);
        }
    }

    public static async Task TerminateDatabaseConnectionsAsync(
        string adminConnectionString,
        string databaseName,
        CancellationToken ct
    )
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = "postgres",
        };

        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync(ct);

        const string terminateSql =
            "SELECT pg_terminate_backend(pid)"
            + " FROM pg_stat_activity"
            + " WHERE datname = @databaseName"
            + " AND pid <> pg_backend_pid();";

        await using var command = new NpgsqlCommand(terminateSql, connection);
        _ = command.Parameters.AddWithValue("@databaseName", databaseName);
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    public static string ExtractDatabaseName(string databaseNameOrConnectionString)
    {
        if (!databaseNameOrConnectionString.Contains(';', StringComparison.Ordinal))
        {
            return databaseNameOrConnectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder(databaseNameOrConnectionString);
        return string.IsNullOrWhiteSpace(builder.Database)
            ? throw new ArgumentException(
                "Connection string does not contain a Database value.",
                nameof(databaseNameOrConnectionString)
            )
            : builder.Database;
    }

    public static void ValidateIdentifier(string identifier)
    {
        if (identifier.Length is 0 or > 63)
        {
            throw new ArgumentException(
                "PostgreSQL identifier length must be between 1 and 63 characters.",
                nameof(identifier)
            );
        }

        foreach (var ch in identifier)
        {
            if (!char.IsAsciiLetterOrDigit(ch) && ch != '_')
            {
                throw new ArgumentException(
                    "PostgreSQL identifier may only contain ASCII letters, digits, or underscores.",
                    nameof(identifier)
                );
            }
        }
    }

    public static string FindRepositoryRoot()
    {
        string[] candidateStarts = [AppContext.BaseDirectory, Directory.GetCurrentDirectory()];

        foreach (var start in candidateStarts)
        {
            var found = SearchUpwardsForSolution(start);
            if (found is not null)
            {
                return found;
            }
        }

        throw new DirectoryNotFoundException(
            "Unable to locate repository root containing Ruth.slnx."
        );
    }

    private static string? SearchUpwardsForSolution(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Ruth.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static async Task<HashSet<string>> GetAvailableExtensionsAsync(
        NpgsqlConnection connection,
        CancellationToken ct
    )
    {
        const string query =
            "SELECT name" + " FROM pg_available_extensions" + " WHERE name = ANY(@names);";

        await using var command = new NpgsqlCommand(query, connection);
        _ = command.Parameters.AddWithValue("@names", _wave0Extensions);

        var available = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            available.Add(reader.GetString(0));
        }

        return available;
    }

    private static string ResolveExtensionSql(string extensionName)
    {
        return extensionName switch
        {
            "uuid-ossp" => """CREATE EXTENSION IF NOT EXISTS "uuid-ossp";""",
            "postgis" => "CREATE EXTENSION IF NOT EXISTS postgis;",
            "btree_gist" => "CREATE EXTENSION IF NOT EXISTS btree_gist;",
            "vector" => "CREATE EXTENSION IF NOT EXISTS vector;",
            "age" => "CREATE EXTENSION IF NOT EXISTS age; LOAD 'age';",
            _ => throw new ArgumentOutOfRangeException(
                nameof(extensionName),
                extensionName,
                "Unsupported PostgreSQL extension."
            ),
        };
    }
}
