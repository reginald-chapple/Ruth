using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Following;

/// <summary>Query to list active FollowingSubscriptions for a person.</summary>
/// <param name="FollowerPersonId">Follower Person identifier.</param>
public sealed record GetPersonFollowingsQuery(Guid FollowerPersonId)
    : IQuery<IReadOnlyList<FollowingSubscriptionResponse>>;
