using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Command to evaluate due Reminders and emit NotificationMessage bell items.</summary>
public sealed record ProcessDueRemindersCommand : ICommand<ReminderProcessRunResponse>;
