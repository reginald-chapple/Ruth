namespace Ruth.Persistence.Outbox;

internal static class OutboxSqlConstants
{
    public const string OutboxTable = "outbox_messages";
    public const string InboxTable = "inbox_messages";

    public const string ColumnTenantId = "tenant_id";
    public const string ColumnId = "id";
    public const string ColumnEventType = "event_type";
    public const string ColumnPayload = "payload";
    public const string ColumnOccurredOnUtc = "occurred_on_utc";
    public const string ColumnProcessedOnUtc = "processed_on_utc";
    public const string ColumnDeadLetteredOnUtc = "dead_lettered_on_utc";
    public const string ColumnRetryCount = "retry_count";
    public const string ColumnNextRetryOnUtc = "next_retry_on_utc";
    public const string ColumnLockedUntilUtc = "locked_until_utc";
    public const string ColumnLockToken = "lock_token";
    public const string ColumnError = "error";

    public const string ColumnMessageId = "message_id";
    public const string ColumnConsumerName = "consumer_name";
}
