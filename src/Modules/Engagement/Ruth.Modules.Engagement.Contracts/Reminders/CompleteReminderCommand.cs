using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Command to mark a Reminder complete.</summary>
/// <param name="ReminderId">Target Reminder identifier.</param>
public sealed record CompleteReminderCommand(Guid ReminderId) : ICommand<ReminderResponse>;
