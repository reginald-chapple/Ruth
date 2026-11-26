namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Staff moderation decision applied to a SupportRequest.</summary>
public enum SupportModerationDecision
{
    /// <summary>Approves the request (optionally with edited text or adjusted visibility).</summary>
    Approve = 0,

    /// <summary>Places the request on Flagged hold.</summary>
    Flag = 1,

    /// <summary>Archives the request.</summary>
    Archive = 2,
}
