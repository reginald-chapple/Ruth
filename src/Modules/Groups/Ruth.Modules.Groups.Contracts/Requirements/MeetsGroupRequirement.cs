namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Evaluation state of a GroupMemberRequirement.</summary>
public enum MeetsGroupRequirement
{
    /// <summary>Requirement is not currently met.</summary>
    NotMet = 0,

    /// <summary>Requirement is met.</summary>
    Meets = 1,

    /// <summary>Requirement is met but nearing expiration or flagged with a warning.</summary>
    MeetsWithWarning = 2,

    /// <summary>Requirement does not apply to this member's role or age classification.</summary>
    NotApplicable = 3,

    /// <summary>Requirement check encountered an evaluation error.</summary>
    Error = 4,
}
