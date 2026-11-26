using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Query to list ReminderTypes.</summary>
/// <param name="ActiveOnly">Whether to return only active types.</param>
public sealed record GetReminderTypesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<ReminderTypeResponse>>;
