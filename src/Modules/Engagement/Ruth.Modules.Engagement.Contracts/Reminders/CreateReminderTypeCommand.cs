using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Command to create a ReminderType.</summary>
/// <param name="Spec">ReminderType specification.</param>
public sealed record CreateReminderTypeCommand(ReminderTypeSpec Spec)
    : ICommand<ReminderTypeResponse>;
