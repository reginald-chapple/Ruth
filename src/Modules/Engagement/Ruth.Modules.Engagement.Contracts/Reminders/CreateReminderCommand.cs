using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Command to create a one-time or recurring entity Reminder.</summary>
/// <param name="ReminderTypeId">ReminderType identifier.</param>
/// <param name="AssigneePersonId">Assigned Person identifier.</param>
/// <param name="EntityTypeKey">Target entity type key.</param>
/// <param name="EntityId">Target entity identifier.</param>
/// <param name="EntitySummary">Target entity display summary.</param>
/// <param name="Note">Reminder note.</param>
/// <param name="ReminderDate">Due calendar date.</param>
/// <param name="RenewPeriodDays">Optional recurring renewal interval in days.</param>
/// <param name="RenewMaxCount">Optional max renewal cycles.</param>
public sealed record CreateReminderCommand(
    Guid ReminderTypeId,
    Guid AssigneePersonId,
    string EntityTypeKey,
    Guid EntityId,
    string EntitySummary,
    string Note,
    DateOnly ReminderDate,
    int? RenewPeriodDays = null,
    int? RenewMaxCount = null
) : ICommand<ReminderResponse>;
