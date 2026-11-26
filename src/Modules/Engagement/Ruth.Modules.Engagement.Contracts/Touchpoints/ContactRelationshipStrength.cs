namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Qualitative relationship depth between a portfolio owner and a constituent.</summary>
public enum ContactRelationshipStrength
{
    /// <summary>Newly assigned or introductory relationship.</summary>
    New = 0,

    /// <summary>Developing rapport and regular check-ins.</summary>
    Developing = 1,

    /// <summary>Established ongoing relationship.</summary>
    Established = 2,

    /// <summary>Strong, high-trust relationship.</summary>
    Strong = 3,

    /// <summary>Relationship showing signs of disengagement or risk.</summary>
    AtRisk = 4,
}
