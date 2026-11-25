using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Primitives;

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
        var effectiveBatchSize = Math.Max(1, batchSize);
        var normalizedNowUtc = nowUtc.ToUniversalTime();
        var lockToken = StronglyTypedId.NextGuid(normalizedNowUtc);

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = CreateClaimCommand(
                (NpgsqlConnection)connection,
                (NpgsqlTransaction)transaction,
                validatedSchema,
                effectiveBatchSize,
                normalizedNowUtc,
                lockedUntilUtc.ToUniversalTime(),
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

            await transaction.CommitAsync(cancellationToken);
            return claimed;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static NpgsqlCommand CreateClaimCommand(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string validatedSchema,
        int batchSize,
        DateTimeOffset nowUtc,
        DateTimeOffset lockedUntilUtc,
        Guid lockToken
    )
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = BuildClaimSql(validatedSchema);

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

    private static string BuildClaimSql(string schema)
    {
        return string.Join(
            '\n',
            "WITH claimed AS (",
            $"    SELECT {OutboxSqlConstants.ColumnId}",
            $"    FROM \"{schema}\".\"{OutboxSqlConstants.OutboxTable}\"",
            $"    WHERE {OutboxSqlConstants.ColumnProcessedOnUtc} IS NULL",
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
