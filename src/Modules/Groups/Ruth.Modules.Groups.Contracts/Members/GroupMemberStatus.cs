namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Lifecycle status of a GroupMember.</summary>
public enum GroupMemberStatus
{
    /// <summary>Member is no longer actively participating.</summary>
    Inactive = 0,

    /// <summary>Member is currently active in the group.</summary>
    Active = 1,

    /// <summary>Member is pending approval or onboarding.</summary>
    Pending = 2,
}
