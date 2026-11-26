using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Following;

/// <summary>Command to consolidate pending FollowingEventNotifications into follower digest notifications.</summary>
public sealed record DispatchFollowingDigestsCommand : ICommand<FollowingDigestRunResponse>;
