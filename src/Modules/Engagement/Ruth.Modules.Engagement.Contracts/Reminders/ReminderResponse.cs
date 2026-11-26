namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Represents an entity follow-up Reminder.</summary>
/// <param name="Id">Reminder identifier.</param>
/// <param name="ReminderTypeId">ReminderType identifier.</param>
/// <param name="ReminderTypeName">ReminderType display name.</param>
/// <param name="HighlightColorHex">Highlight color hex.</param>
/// <param name="AssigneePersonId">Assigned Person identifier.</param>
/// <param name="EntityTypeKey">Target entity type key.</param>
/// <param name="EntityId">Target entity identifier.</param>
/// <param name="EntitySummary">Target entity display summary.</param>
/// <param name="Note">Reminder note.</param>
/// <param name="ReminderDate">Due calendar date.</param>
/// <param name="RenewPeriodDays">Optional recurring renewal interval in days.</param>
/// <param name="RenewMaxCount">Optional maximum renewal count.</param>
/// <param name="RenewCurrentCount">Completed renewal cycles.</param>
/// <param name="IsComplete">Whether completed.</param>
/// <param name="CompletedAtUtc">Optional completion UTC timestamp.</param>
/// <param name="LastNotifiedAtUtc">Optional last notification UTC timestamp.</param>
public sealed record ReminderResponse(
    Guid Id,
    Guid ReminderTypeId,
    string ReminderTypeName,
    string HighlightColorHex,
    Guid AssigneePersonId,
    string EntityTypeKey,
    Guid EntityId,
    string EntitySummary,
    string Note,
    DateOnly ReminderDate,
    int? RenewPeriodDays,
    int? RenewMaxCount,
    int RenewCurrentCount,
    bool IsComplete,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? LastNotifiedAtUtc
);
