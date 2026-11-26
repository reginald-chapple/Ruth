using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Persistence.Outbox;

internal static class OutboxBatchClaimer
{
    public static async Task<IReadOnlyList<OutboxClaimedMessage>> ClaimBatchAsync(
        ModuleDbContext dbContext,
        string schema,
        int batchSize,
        DateTimeOffset nowUtc,
        DateTimeOffset lockedUntilUtc,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        var validatedSchema = SchemaValidator.Validate(schema);
        var isTenantScoped = dbContext.RequiresTenantIsolation;
        var tenantId = dbContext.CurrentTenantId;

        if (isTenantScoped && tenantId.IsEmpty)
        {
            throw new TenantContextRequiredException(
                $"Cannot claim Outbox messages on '{dbContext.GetType().Name}' without an active tenant context."
            );
        }

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            return await ExecuteClaimTransactionAsync(
                dbContext,
                (NpgsqlConnection)connection,
                validatedSchema,
                isTenantScoped,
                tenantId,
                Math.Max(1, batchSize),
                nowUtc.ToUniversalTime(),
                lockedUntilUtc.ToUniversalTime(),
                cancellationToken
            );
        }
        finally
        {
            if (shouldClose)
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }

    private static async Task<IReadOnlyList<OutboxClaimedMessage>> ExecuteClaimTransactionAsync(
        ModuleDbContext dbContext,
        NpgsqlConnection connection,
        string validatedSchema,
        bool isTenantScoped,
        TenantId tenantId,
        int effectiveBatchSize,
        DateTimeOffset normalizedNowUtc,
        DateTimeOffset normalizedLockedUntilUtc,
        CancellationToken cancellationToken
    )
    {
        var lockToken = StronglyTypedId.NextGuid(normalizedNowUtc);
        await using var efTransaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );
        var dbTransaction = (NpgsqlTransaction)efTransaction.GetDbTransaction();
        await TenantRlsConnectionSynchronizer.EnsureCurrentTenantAsync(
            connection,
            dbTransaction,
            cancellationToken
        );

        await using var command = CreateClaimCommand(
            connection,
            dbTransaction,
            validatedSchema,
            isTenantScoped,
            tenantId,
            effectiveBatchSize,
            normalizedNowUtc,
            normalizedLockedUntilUtc,
            lockToken
        );

        var claimed = new List<OutboxClaimedMessage>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                claimed.Add(ReadMessage(reader));
            }
        }

        await efTransaction.CommitAsync(cancellationToken);
        return claimed;
    }

    private static NpgsqlCommand CreateClaimCommand(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string validatedSchema,
        bool isTenantScoped,
        TenantId tenantId,
        int batchSize,
        DateTimeOffset nowUtc,
        DateTimeOffset lockedUntilUtc,
        Guid lockToken
    )
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = BuildClaimSql(validatedSchema, isTenantScoped);

        if (isTenantScoped)
        {
            _ = command.Parameters.Add(
                new NpgsqlParameter("tenant_id", NpgsqlDbType.Uuid) { Value = tenantId.Value }
            );
        }

        _ = command.Parameters.Add(
            new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = nowUtc }
        );
        _ = command.Parameters.Add(
            new NpgsqlParameter("batch_size", NpgsqlDbType.Integer) { Value = batchSize }
        );
        _ = command.Parameters.Add(
            new NpgsqlParameter("locked_until", NpgsqlDbType.TimestampTz) { Value = lockedUntilUtc }
        );
        _ = command.Parameters.Add(
            new NpgsqlParameter("lock_token", NpgsqlDbType.Uuid) { Value = lockToken }
        );

        return command;
    }

    private static string BuildClaimSql(string schema, bool isTenantScoped)
    {
        var tenantPredicate = isTenantScoped
            ? $"    WHERE {OutboxSqlConstants.ColumnTenantId} = @tenant_id\n      AND {OutboxSqlConstants.ColumnProcessedOnUtc} IS NULL"
            : $"    WHERE {OutboxSqlConstants.ColumnProcessedOnUtc} IS NULL";

        return string.Join(
            '\n',
            "WITH claimed AS (",
            $"    SELECT {OutboxSqlConstants.ColumnId}",
            $"    FROM \"{schema}\".\"{OutboxSqlConstants.OutboxTable}\"",
            tenantPredicate,
            $"      AND {OutboxSqlConstants.ColumnDeadLetteredOnUtc} IS NULL",
            $"      AND ({OutboxSqlConstants.ColumnNextRetryOnUtc} IS NULL OR {OutboxSqlConstants.ColumnNextRetryOnUtc} <= @now)",
            $"      AND ({OutboxSqlConstants.ColumnLockedUntilUtc} IS NULL OR {OutboxSqlConstants.ColumnLockedUntilUtc} <= @now)",
            $"    ORDER BY {OutboxSqlConstants.ColumnOccurredOnUtc}, {OutboxSqlConstants.ColumnId}",
            "    LIMIT @batch_size",
            "    FOR UPDATE SKIP LOCKED",
            ")",
            $"UPDATE \"{schema}\".\"{OutboxSqlConstants.OutboxTable}\" AS o",
            $"SET {OutboxSqlConstants.ColumnLockedUntilUtc} = @locked_until,",
            $"    {OutboxSqlConstants.ColumnLockToken} = @lock_token",
            "FROM claimed",
            $"WHERE o.{OutboxSqlConstants.ColumnId} = claimed.{OutboxSqlConstants.ColumnId}",
            $"RETURNING o.{OutboxSqlConstants.ColumnId},",
            $"          o.{OutboxSqlConstants.ColumnEventType},",
            $"          o.{OutboxSqlConstants.ColumnPayload},",
            $"          o.{OutboxSqlConstants.ColumnRetryCount},",
            $"          o.{OutboxSqlConstants.ColumnLockToken};"
        );
    }

    private static OutboxClaimedMessage ReadMessage(NpgsqlDataReader reader)
    {
        return new OutboxClaimedMessage(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetGuid(4)
        );
    }
}
