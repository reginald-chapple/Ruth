using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Reporting.Infrastructure.DataViews;

internal static class ReportingCrossSchemaSqlRunner
{
    public static Task<bool> TableExistsAsync(
        ReportingDbContext db,
        string qualifiedTableName,
        CancellationToken ct
    )
    {
        return WithConnectionAsync(
            db,
            async conn =>
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT to_regclass(@name) IS NOT NULL";
                _ = cmd.Parameters.Add(new NpgsqlParameter("name", qualifiedTableName));
                var scalar = await cmd.ExecuteScalarAsync(ct);
                return scalar is bool b && b;
            },
            ct
        );
    }

    public static Task<IReadOnlyList<DataViewEntityRecord>> ExecuteRecordsQueryAsync(
        ReportingDbContext db,
        string sql,
        int maxRows,
        Func<DbDataReader, DataViewEntityRecord> mapRow,
        CancellationToken ct
    )
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        return tenantId.IsEmpty
            ? Task.FromResult<IReadOnlyList<DataViewEntityRecord>>([])
            : WithConnectionAsync<IReadOnlyList<DataViewEntityRecord>>(
                db,
                async conn =>
                {
                    await using var cmd = CreateTenantCommand(conn, sql, tenantId);
                    _ = cmd.Parameters.Add(
                        new NpgsqlParameter("limit", Math.Clamp(maxRows, 1, 5000))
                    );

                    List<DataViewEntityRecord> results = [];
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        results.Add(mapRow(reader));
                    }

                    return results;
                },
                ct
            );
    }

    public static async Task<IReadOnlySet<Guid>> QueryGuidSetIfTableExistsAsync(
        ReportingDbContext db,
        string tableName,
        string sql,
        Guid? parameter,
        CancellationToken ct
    )
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        return tenantId.IsEmpty || !await TableExistsAsync(db, tableName, ct)
            ? new HashSet<Guid>()
            : await WithConnectionAsync<IReadOnlySet<Guid>>(
                db,
                async conn =>
                {
                    await using var cmd = CreateTenantCommand(conn, sql, tenantId);
                    if (parameter.HasValue)
                    {
                        _ = cmd.Parameters.Add(new NpgsqlParameter("p0", parameter.Value));
                    }

                    var set = new HashSet<Guid>();
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        if (!await reader.IsDBNullAsync(0, ct))
                        {
                            _ = set.Add(reader.GetGuid(0));
                        }
                    }

                    return set;
                },
                ct
            );
    }

    public static async Task ReadTenantRowsAsync(
        ReportingDbContext db,
        string sql,
        Func<DbDataReader, Task> onRow,
        CancellationToken ct
    )
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (tenantId.IsEmpty)
        {
            return;
        }

        _ = await WithConnectionAsync(
            db,
            async conn =>
            {
                await using var cmd = CreateTenantCommand(conn, sql, tenantId);
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    await onRow(reader);
                }

                return true;
            },
            ct
        );
    }

    public static async Task ExecuteWithTenantAsync(
        ReportingDbContext db,
        Func<DbConnection, TenantId, Task> action,
        CancellationToken ct
    )
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (tenantId.IsEmpty)
        {
            return;
        }

        _ = await WithConnectionAsync(
            db,
            async conn =>
            {
                await action(conn, tenantId);
                return true;
            },
            ct
        );
    }

    public static async Task<decimal> ScalarDecimalIfExistsAsync(
        ReportingDbContext db,
        string tableName,
        string sql,
        CancellationToken ct
    )
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        return tenantId.IsEmpty || !await TableExistsAsync(db, tableName, ct)
            ? 0m
            : await WithConnectionAsync(
                db,
                async conn =>
                {
                    await using var cmd = CreateTenantCommand(conn, sql, tenantId);
                    var val = await cmd.ExecuteScalarAsync(ct);
                    return val is decimal d ? d : 0m;
                },
                ct
            );
    }

    public static async Task<decimal> ScalarDecimalWithRangeAsync(
        ReportingDbContext db,
        string tableName,
        string sql,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken ct
    )
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        return tenantId.IsEmpty || !await TableExistsAsync(db, tableName, ct)
            ? 0m
            : await WithConnectionAsync(
                db,
                async conn =>
                {
                    await using var cmd = CreateTenantCommand(conn, sql, tenantId);
                    _ = cmd.Parameters.Add(new NpgsqlParameter("startUtc", startUtc));
                    _ = cmd.Parameters.Add(new NpgsqlParameter("endUtc", endUtc));
                    var val = await cmd.ExecuteScalarAsync(ct);
                    return val is decimal d ? d : 0m;
                },
                ct
            );
    }

    public static DbCommand CreateTenantCommand(DbConnection conn, string sql, TenantId tenantId)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        _ = cmd.Parameters.Add(
            new NpgsqlParameter("tenantId", tenantId.Value)
            {
                NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid,
            }
        );
        return cmd;
    }

    public static Dictionary<string, string?>? ParseJsonbDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
            return parsed is null
                ? null
                : new Dictionary<string, string?>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task<T> WithConnectionAsync<T>(
        ReportingDbContext db,
        Func<DbConnection, Task<T>> action,
        CancellationToken ct
    )
    {
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != ConnectionState.Open;
        if (shouldClose)
        {
            await db.Database.OpenConnectionAsync(ct);
        }

        try
        {
            await TenantRlsConnectionSynchronizer.EnsureCurrentTenantAsync(
                conn,
                db.Database.CurrentTransaction?.GetDbTransaction(),
                ct
            );
            return await action(conn);
        }
        finally
        {
            if (shouldClose)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }
}
