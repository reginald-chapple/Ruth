namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Privacy and sharing visibility level for a SupportRequest.</summary>
public enum SupportVisibilityLevel
{
    /// <summary>Visible strictly to authorized care-team coordinators.</summary>
    PrivateCareTeamOnly = 0,

    /// <summary>Visible to authenticated organization members.</summary>
    RegisteredMembers = 1,

    /// <summary>Visible on the public Community Care / Support Wall once moderated.</summary>
    PublicCommunityWall = 2,
}
