namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Qualitative outcome sentiment recorded upon completing a Touchpoint.</summary>
public enum TouchpointOutcomeSentiment
{
    /// <summary>Warm, engaged interaction.</summary>
    Positive = 0,

    /// <summary>Routine informational check-in.</summary>
    Neutral = 1,

    /// <summary>Constituent expressed a need or follow-up action.</summary>
    NeedsFollowUp = 2,

    /// <summary>Left voicemail / no response yet.</summary>
    NoResponse = 3,
}
