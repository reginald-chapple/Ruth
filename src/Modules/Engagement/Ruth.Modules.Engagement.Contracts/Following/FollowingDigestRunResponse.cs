namespace Ruth.Modules.Engagement.Contracts.Following;

/// <summary>Summary of consolidated Following digest notifications dispatched.</summary>
/// <param name="DigestsSentCount">Number of follower digests dispatched.</param>
/// <param name="EventsIncludedCount">Number of entity events consolidated.</param>
public sealed record FollowingDigestRunResponse(int DigestsSentCount, int EventsIncludedCount);
