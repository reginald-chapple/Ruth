namespace Ruth.Modules.Engagement.Contracts.Following;

/// <summary>Represents a constituent/staff FollowingSubscription on a target entity.</summary>
/// <param name="Id">Subscription identifier.</param>
/// <param name="FollowerPersonId">Follower Person identifier.</param>
/// <param name="EntityTypeKey">Followed entity type key.</param>
/// <param name="EntityId">Followed entity identifier.</param>
/// <param name="EntityDisplayName">Followed entity display name.</param>
/// <param name="PurposeKey">Subscription purpose key.</param>
/// <param name="IsSubscribed">Whether actively subscribed.</param>
/// <param name="LastNotifiedAtUtc">Optional last digest notification timestamp.</param>
public sealed record FollowingSubscriptionResponse(
    Guid Id,
    Guid FollowerPersonId,
    string EntityTypeKey,
    Guid EntityId,
    string EntityDisplayName,
    string PurposeKey,
    bool IsSubscribed,
    DateTimeOffset? LastNotifiedAtUtc
);
