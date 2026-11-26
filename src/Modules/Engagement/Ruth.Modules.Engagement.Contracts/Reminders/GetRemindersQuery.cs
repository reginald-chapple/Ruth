using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Query to retrieve Reminders filtered by assignee, entity, or completion state.</summary>
/// <param name="AssigneePersonId">Optional assignee Person filter.</param>
/// <param name="EntityTypeKey">Optional entity type key filter.</param>
/// <param name="EntityId">Optional entity ID filter.</param>
/// <param name="IncludeCompleted">Whether to include completed reminders.</param>
public sealed record GetRemindersQuery(
    Guid? AssigneePersonId = null,
    string? EntityTypeKey = null,
    Guid? EntityId = null,
    bool IncludeCompleted = false
) : IQuery<IReadOnlyList<ReminderResponse>>;
