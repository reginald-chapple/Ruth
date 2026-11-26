using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Following;

/// <summary>Command to subscribe or unsubscribe from Following updates on an entity.</summary>
/// <param name="FollowerPersonId">Follower Person identifier.</param>
/// <param name="EntityTypeKey">Target entity type key.</param>
/// <param name="EntityId">Target entity identifier.</param>
/// <param name="EntityDisplayName">Target entity display name.</param>
/// <param name="PurposeKey">Optional purpose key (defaults to 'default').</param>
/// <param name="Subscribe">True to follow; false to unfollow/mute.</param>
public sealed record ToggleFollowingSubscriptionCommand(
    Guid FollowerPersonId,
    string EntityTypeKey,
    Guid EntityId,
    string EntityDisplayName,
    string PurposeKey = "default",
    bool Subscribe = true
) : ICommand<FollowingSubscriptionResponse>;
