using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Following;

/// <summary>Command to record a notable event on a followed entity for upcoming digest delivery.</summary>
/// <param name="EntityTypeKey">Entity type key.</param>
/// <param name="EntityId">Entity identifier.</param>
/// <param name="EventTypeKey">Notable event type key.</param>
/// <param name="Summary">Human-readable event summary.</param>
public sealed record RecordFollowedEntityEventCommand(
    string EntityTypeKey,
    Guid EntityId,
    string EventTypeKey,
    string Summary
) : ICommand<Guid>;
