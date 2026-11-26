namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Lifecycle status of a peer-to-peer, team, or project fundraising campaign.</summary>
public enum FundraisingCampaignStatus
{
    /// <summary>Draft campaign not yet open for public contributions.</summary>
    Draft = 0,

    /// <summary>Active fundraising campaign accepting contributions.</summary>
    Active = 1,

    /// <summary>Completed fundraising campaign.</summary>
    Completed = 2,

    /// <summary>Archived campaign.</summary>
    Archived = 3,
}
