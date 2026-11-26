using System.Globalization;
using Npgsql;
using NpgsqlTypes;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

internal static class Tier2SqlHelpers
{
    private static readonly Guid _defaultTenantGuid = new(
        0x01967000,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x01
    );

    public static async Task<NpgsqlConnection> OpenConnectionAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static async Task InsertInboxAsync(
        NpgsqlConnection connection,
        Guid messageId,
        string consumerName
    )
    {
        const string sql =
            "INSERT INTO identity.inbox_messages (tenant_id, message_id, consumer_name, processed_on_utc)"
            + " VALUES (@tenantId, @messageId, @consumerName, @processedOnUtc);";
        await using var cmd = new NpgsqlCommand(sql, connection);
        _ = cmd.Parameters.AddWithValue("@tenantId", _defaultTenantGuid);
        _ = cmd.Parameters.AddWithValue("@messageId", messageId);
        _ = cmd.Parameters.AddWithValue("@consumerName", consumerName);
        _ = cmd.Parameters.AddWithValue("@processedOnUtc", DateTimeOffset.UtcNow);
        _ = await cmd.ExecuteNonQueryAsync();
    }

    public static async Task InsertOutboxAsync(
        NpgsqlConnection connection,
        Guid id,
        string eventType,
        string payloadJson,
        DateTimeOffset occurredOnUtc,
        DateTimeOffset? processedOnUtc = null
    )
    {
        const string sql =
            "INSERT INTO identity.outbox_messages"
            + " (id, tenant_id, event_type, payload, occurred_on_utc, processed_on_utc)"
            + " VALUES (@id, @tenantId, @eventType, @payload, @occurredOnUtc, @processedOnUtc);";
        await using var cmd = new NpgsqlCommand(sql, connection);
        _ = cmd.Parameters.AddWithValue("@id", id);
        _ = cmd.Parameters.AddWithValue("@tenantId", _defaultTenantGuid);
        _ = cmd.Parameters.AddWithValue("@eventType", eventType);
        _ = cmd.Parameters.Add(
            new NpgsqlParameter("@payload", NpgsqlDbType.Jsonb) { Value = payloadJson }
        );
        _ = cmd.Parameters.AddWithValue("@occurredOnUtc", occurredOnUtc);
        _ = cmd.Parameters.AddWithValue("@processedOnUtc", (object?)processedOnUtc ?? DBNull.Value);
        _ = await cmd.ExecuteNonQueryAsync();
    }

    public static async Task InsertOutboxCastJsonbAsync(
        NpgsqlConnection connection,
        Guid id,
        string eventType,
        string rawPayload
    )
    {
        const string sql =
            "INSERT INTO identity.outbox_messages (id, tenant_id, event_type, payload, occurred_on_utc)"
            + " VALUES (@id, @tenantId, @eventType, CAST(@payload AS jsonb), @occurredOnUtc);";
        await using var cmd = new NpgsqlCommand(sql, connection);
        _ = cmd.Parameters.AddWithValue("@id", id);
        _ = cmd.Parameters.AddWithValue("@tenantId", _defaultTenantGuid);
        _ = cmd.Parameters.AddWithValue("@eventType", eventType);
        _ = cmd.Parameters.AddWithValue("@payload", rawPayload);
        _ = cmd.Parameters.AddWithValue("@occurredOnUtc", DateTimeOffset.UtcNow);
        _ = await cmd.ExecuteNonQueryAsync();
    }

    public static async Task AssertOutboxRetryCountZeroAsync(DatabaseProbe probe, Guid id)
    {
        var row = (await probe.ReadIdentityOutboxMessagesAsync()).Single(r =>
            DatabaseProbe.TryGetColumnValue(r, "Id", out var v) && Equals(v, id)
        );
        Assert.True(DatabaseProbe.TryGetColumnValue(row, "RetryCount", out var retry));
        Assert.Equal(0, Convert.ToInt32(retry, CultureInfo.InvariantCulture));
    }

    public static async Task InsertScheduledJobAsync(
        NpgsqlConnection connection,
        Guid id,
        string? jobKey,
        string cronExpression,
        DateTimeOffset? nextRunUtc = null
    )
    {
        const string sql =
            "INSERT INTO scheduler.scheduled_jobs"
            + " (id, job_key, name, cron_expression, is_active, next_run_utc)"
            + " VALUES (@id, @jobKey, @name, @cron, true, @nextRunUtc);";
        await using var cmd = new NpgsqlCommand(sql, connection);
        _ = cmd.Parameters.AddWithValue("@id", id);
        _ = cmd.Parameters.AddWithValue("@jobKey", (object?)jobKey ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("@name", "Tier2 Boundary Job");
        _ = cmd.Parameters.AddWithValue("@cron", cronExpression);
        _ = cmd.Parameters.AddWithValue("@nextRunUtc", (object?)nextRunUtc ?? DBNull.Value);
        _ = await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<bool> IsDeadLetteredWithErrorAsync(DatabaseProbe probe, Guid id)
    {
        var rows = await probe.ReadIdentityOutboxMessagesAsync();
        var row = rows.FirstOrDefault(r =>
            DatabaseProbe.TryGetColumnValue(r, "Id", out var v) && Equals(v, id)
        );
        if (row is null)
        {
            return false;
        }

        _ = DatabaseProbe.TryGetColumnValue(row, "DeadLetteredOnUtc", out var deadLettered);
        _ = DatabaseProbe.TryGetColumnValue(row, "ProcessedOnUtc", out var processed);
        _ = DatabaseProbe.TryGetColumnValue(row, "RetryCount", out var retries);
        _ = DatabaseProbe.TryGetColumnValue(row, "Error", out var err);
        var count = Convert.ToInt32(retries, CultureInfo.InvariantCulture);
        return deadLettered is not null
            && processed is null
            && count == 0
            && err is string s
            && !string.IsNullOrWhiteSpace(s);
    }

    public static async Task<bool> IsOutboxProcessedWithRetriesAsync(
        DatabaseProbe probe,
        Guid id,
        int minRetries
    )
    {
        var rows = await probe.ReadIdentityOutboxMessagesAsync();
        var row = rows.FirstOrDefault(r =>
            DatabaseProbe.TryGetColumnValue(r, "Id", out var v) && Equals(v, id)
        );
        if (row is null)
        {
            return false;
        }

        _ = DatabaseProbe.TryGetColumnValue(row, "ProcessedOnUtc", out var processed);
        _ = DatabaseProbe.TryGetColumnValue(row, "RetryCount", out var retries);
        var count = Convert.ToInt32(retries, CultureInfo.InvariantCulture);
        return processed is not null && count >= minRetries;
    }

    public static async Task<DateTimeOffset?> TryGetFirstProcessedTimestampAsync(
        DatabaseProbe probe
    )
    {
        var rows = await probe.ReadIdentityOutboxMessagesAsync();
        return
            rows.Count > 0
            && DatabaseProbe.TryGetColumnValue(rows[0], "ProcessedOnUtc", out var val)
            && val is not null
            ? Tier2TestHelpers.ConvertUtcTimestamp(val)
            : null;
    }

    public static void AssertCreatedOnUtcInRange(
        IReadOnlyDictionary<string, object?> row,
        DateTimeOffset minUtc,
        DateTimeOffset maxUtc
    )
    {
        Assert.True(DatabaseProbe.TryGetColumnValue(row, "CreatedOnUtc", out var raw));
        var timestamp = Tier2TestHelpers.ConvertUtcTimestamp(raw);
        TestOnboardingPayloads.AssertUtcTimestampInRange(timestamp, minUtc, maxUtc);
    }

    public static NpgsqlCommand CreateDuplicateUserInsertCommand(
        NpgsqlConnection connection,
        string userTable,
        Guid existingId
    )
    {
        var uniqueSuffix = Guid.CreateVersion7().ToString("N");
        var sql = userTable switch
        {
            "UserAccounts" =>
                "INSERT INTO \"identity\".\"UserAccounts\" (\"Id\", tenant_id, \"UserName\","
                    + " \"NormalizedUserName\", \"Email\", \"NormalizedEmail\", \"DisplayName\","
                    + " \"PasswordHash\", \"IsConfirmed\", \"IsLockedOut\", \"CreatedOnUtc\","
                    + " \"ConcurrencyStamp\") VALUES (@id, @tenantId, @userName, @normalizedUserName, @email,"
                    + " @normalizedEmail, @displayName, @passwordHash, @isConfirmed, @isLockedOut,"
                    + " @createdOnUtc, @concurrencyStamp);",
            "user_accounts" =>
                "INSERT INTO identity.user_accounts (id, tenant_id, user_name, normalized_user_name, email,"
                    + " normalized_email, display_name, password_hash, is_confirmed, is_locked_out,"
                    + " created_on_utc, concurrency_stamp) VALUES (@id, @tenantId, @userName,"
                    + " @normalizedUserName, @email, @normalizedEmail, @displayName, @passwordHash,"
                    + " @isConfirmed, @isLockedOut, @createdOnUtc, @concurrencyStamp);",
            _ => throw new NotSupportedException($"Unexpected user table '{userTable}'."),
        };
        var cmd = new NpgsqlCommand(sql, connection);
        var userName = $"dup_{uniqueSuffix[..12]}";
        var email = $"{userName}@gracechurch.test";
        _ = cmd.Parameters.AddWithValue("@id", existingId);
        _ = cmd.Parameters.AddWithValue("@tenantId", _defaultTenantGuid);
        _ = cmd.Parameters.AddWithValue("@userName", userName);
        _ = cmd.Parameters.AddWithValue("@normalizedUserName", userName.ToUpperInvariant());
        _ = cmd.Parameters.AddWithValue("@email", email);
        _ = cmd.Parameters.AddWithValue("@normalizedEmail", email.ToUpperInvariant());
        _ = cmd.Parameters.AddWithValue("@displayName", "Duplicate PK Probe");
        _ = cmd.Parameters.AddWithValue("@passwordHash", "not-a-real-hash");
        _ = cmd.Parameters.AddWithValue("@isConfirmed", value: true);
        _ = cmd.Parameters.AddWithValue("@isLockedOut", value: false);
        _ = cmd.Parameters.AddWithValue("@createdOnUtc", DateTimeOffset.UtcNow);
        _ = cmd.Parameters.AddWithValue("@concurrencyStamp", Guid.CreateVersion7());
        return cmd;
    }
}
