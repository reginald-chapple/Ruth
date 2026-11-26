namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Moderation and lifecycle state for a SupportRequest.</summary>
public enum SupportModerationState
{
    /// <summary>Awaiting care-team moderation before sharing.</summary>
    PendingReview = 0,

    /// <summary>Approved for its configured visibility scope.</summary>
    Approved = 1,

    /// <summary>Pulled from public display due to community flag threshold or staff hold.</summary>
    Flagged = 2,

    /// <summary>Resolved, expired, or archived.</summary>
    Archived = 3,
}
