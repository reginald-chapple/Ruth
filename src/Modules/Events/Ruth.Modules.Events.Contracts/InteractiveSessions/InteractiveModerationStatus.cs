namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Moderation status of an audience response in a live Q&amp;A or prompt.</summary>
public enum InteractiveModerationStatus
{
    /// <summary>Visible on live stage projections when enabled.</summary>
    Approved = 0,

    /// <summary>Held in moderator queue pending review.</summary>
    Pending = 1,

    /// <summary>Hidden by moderator.</summary>
    Hidden = 2,
}
