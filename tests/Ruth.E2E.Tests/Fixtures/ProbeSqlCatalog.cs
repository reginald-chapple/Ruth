using Npgsql;

namespace Ruth.E2E.Tests.Fixtures;

internal static class ProbeSqlCatalog
{
    public static readonly string[] OutboxTableCandidates =
    [
        "OutboxMessages",
        "outbox_messages",
        "OutboxMessage",
        "outbox_message",
    ];

    public static readonly string[] InboxTableCandidates =
    [
        "InboxMessages",
        "inbox_messages",
        "InboxMessage",
        "inbox_message",
    ];

    public static readonly string[] UserAccountTableCandidates = ["UserAccounts", "user_accounts"];

    public static readonly string[] OrganizationTableCandidates =
    [
        "OrganizationProfiles",
        "organization_profiles",
    ];

    public static readonly string[] ScheduledJobTableCandidates =
    [
        "ScheduledJobs",
        "scheduled_jobs",
    ];

    public static readonly string[] ScheduledJobHistoryTableCandidates =
    [
        "ScheduledJobHistories",
        "scheduled_job_histories",
        "ScheduledJobHistory",
        "scheduled_job_history",
    ];

    public static NpgsqlCommand? TryCreateCountCommand(
        NpgsqlConnection connection,
        string schemaName,
        string tableName
    )
    {
        return schemaName switch
        {
            "identity" => TryCreateIdentityOutboxCountCommand(connection, tableName)
                ?? TryCreateIdentityEntityCountCommand(connection, tableName),
            "scheduler" => TryCreateSchedulerCountCommand(connection, tableName),
            _ => null,
        };
    }

    public static NpgsqlCommand? TryCreateSelectCommand(
        NpgsqlConnection connection,
        string schemaName,
        string tableName
    )
    {
        return schemaName switch
        {
            "identity" => TryCreateIdentityOutboxSelectCommand(connection, tableName)
                ?? TryCreateIdentityEntitySelectCommand(connection, tableName),
            "scheduler" => TryCreateSchedulerSelectCommand(connection, tableName),
            _ => null,
        };
    }

    public static async Task<IReadOnlyList<string>> ReadStringListAsync(
        NpgsqlCommand command,
        CancellationToken ct
    )
    {
        var items = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(reader.GetString(0));
        }

        return items;
    }

    private static NpgsqlCommand? TryCreateIdentityOutboxCountCommand(
        NpgsqlConnection connection,
        string tableName
    )
    {
        var sql = tableName switch
        {
            "OutboxMessages" => """SELECT COUNT(*) FROM "identity"."OutboxMessages";""",
            "outbox_messages" => "SELECT COUNT(*) FROM identity.outbox_messages;",
            "OutboxMessage" => """SELECT COUNT(*) FROM "identity"."OutboxMessage";""",
            "outbox_message" => "SELECT COUNT(*) FROM identity.outbox_message;",
            "InboxMessages" => """SELECT COUNT(*) FROM "identity"."InboxMessages";""",
            "inbox_messages" => "SELECT COUNT(*) FROM identity.inbox_messages;",
            "InboxMessage" => """SELECT COUNT(*) FROM "identity"."InboxMessage";""",
            "inbox_message" => "SELECT COUNT(*) FROM identity.inbox_message;",
            _ => null,
        };

        return sql is null ? null : new NpgsqlCommand(sql, connection);
    }

    private static NpgsqlCommand? TryCreateIdentityEntityCountCommand(
        NpgsqlConnection connection,
        string tableName
    )
    {
        var sql = tableName switch
        {
            "UserAccounts" => """SELECT COUNT(*) FROM "identity"."UserAccounts";""",
            "user_accounts" => "SELECT COUNT(*) FROM identity.user_accounts;",
            "OrganizationProfiles" => """SELECT COUNT(*) FROM "identity"."OrganizationProfiles";""",
            "organization_profiles" => "SELECT COUNT(*) FROM identity.organization_profiles;",
            "SecurityRoles" => """SELECT COUNT(*) FROM "identity"."SecurityRoles";""",
            "security_roles" => "SELECT COUNT(*) FROM identity.security_roles;",
            "UserRoles" => """SELECT COUNT(*) FROM "identity"."UserRoles";""",
            "user_roles" => "SELECT COUNT(*) FROM identity.user_roles;",
            _ => null,
        };

        return sql is null ? null : new NpgsqlCommand(sql, connection);
    }

    private static NpgsqlCommand? TryCreateSchedulerCountCommand(
        NpgsqlConnection connection,
        string tableName
    )
    {
        var sql = tableName switch
        {
            "ScheduledJobs" => """SELECT COUNT(*) FROM "scheduler"."ScheduledJobs";""",
            "scheduled_jobs" => "SELECT COUNT(*) FROM scheduler.scheduled_jobs;",
            "ScheduledJobHistories" =>
                """SELECT COUNT(*) FROM "scheduler"."ScheduledJobHistories";""",
            "scheduled_job_histories" => "SELECT COUNT(*) FROM scheduler.scheduled_job_histories;",
            "ScheduledJobHistory" => """SELECT COUNT(*) FROM "scheduler"."ScheduledJobHistory";""",
            "scheduled_job_history" => "SELECT COUNT(*) FROM scheduler.scheduled_job_history;",
            _ => null,
        };

        return sql is null ? null : new NpgsqlCommand(sql, connection);
    }

    private static NpgsqlCommand? TryCreateIdentityOutboxSelectCommand(
        NpgsqlConnection connection,
        string tableName
    )
    {
        var sql = tableName switch
        {
            "OutboxMessages" => """SELECT * FROM "identity"."OutboxMessages" LIMIT @limit;""",
            "outbox_messages" => "SELECT * FROM identity.outbox_messages LIMIT @limit;",
            "OutboxMessage" => """SELECT * FROM "identity"."OutboxMessage" LIMIT @limit;""",
            "outbox_message" => "SELECT * FROM identity.outbox_message LIMIT @limit;",
            "InboxMessages" => """SELECT * FROM "identity"."InboxMessages" LIMIT @limit;""",
            "inbox_messages" => "SELECT * FROM identity.inbox_messages LIMIT @limit;",
            "InboxMessage" => """SELECT * FROM "identity"."InboxMessage" LIMIT @limit;""",
            "inbox_message" => "SELECT * FROM identity.inbox_message LIMIT @limit;",
            _ => null,
        };

        return sql is null ? null : new NpgsqlCommand(sql, connection);
    }

    private static NpgsqlCommand? TryCreateIdentityEntitySelectCommand(
        NpgsqlConnection connection,
        string tableName
    )
    {
        var sql = tableName switch
        {
            "UserAccounts" => """SELECT * FROM "identity"."UserAccounts" LIMIT @limit;""",
            "user_accounts" => "SELECT * FROM identity.user_accounts LIMIT @limit;",
            "OrganizationProfiles" =>
                """SELECT * FROM "identity"."OrganizationProfiles" LIMIT @limit;""",
            "organization_profiles" => "SELECT * FROM identity.organization_profiles LIMIT @limit;",
            "SecurityRoles" => """SELECT * FROM "identity"."SecurityRoles" LIMIT @limit;""",
            "security_roles" => "SELECT * FROM identity.security_roles LIMIT @limit;",
            "UserRoles" => """SELECT * FROM "identity"."UserRoles" LIMIT @limit;""",
            "user_roles" => "SELECT * FROM identity.user_roles LIMIT @limit;",
            _ => null,
        };

        return sql is null ? null : new NpgsqlCommand(sql, connection);
    }

    private static NpgsqlCommand? TryCreateSchedulerSelectCommand(
        NpgsqlConnection connection,
        string tableName
    )
    {
        var sql = tableName switch
        {
            "ScheduledJobs" => """SELECT * FROM "scheduler"."ScheduledJobs" LIMIT @limit;""",
            "scheduled_jobs" => "SELECT * FROM scheduler.scheduled_jobs LIMIT @limit;",
            "ScheduledJobHistories" =>
                """SELECT * FROM "scheduler"."ScheduledJobHistories" LIMIT @limit;""",
            "scheduled_job_histories" =>
                "SELECT * FROM scheduler.scheduled_job_histories LIMIT @limit;",
            "ScheduledJobHistory" =>
                """SELECT * FROM "scheduler"."ScheduledJobHistory" LIMIT @limit;""",
            "scheduled_job_history" =>
                "SELECT * FROM scheduler.scheduled_job_history LIMIT @limit;",
            _ => null,
        };

        return sql is null ? null : new NpgsqlCommand(sql, connection);
    }
}
