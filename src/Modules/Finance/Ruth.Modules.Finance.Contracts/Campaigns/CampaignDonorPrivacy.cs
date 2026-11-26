namespace Ruth.Modules.Finance.Contracts.Campaigns;

/// <summary>Controls how a donor's identity and gift amount appear on public campaign portals.</summary>
public enum CampaignDonorPrivacy
{
    /// <summary>Display both the donor's name and contribution amount publicly.</summary>
    PublicNameAndAmount = 0,

    /// <summary>Display the donor's name publicly while hiding the contribution amount.</summary>
    PublicNameHideAmount = 1,

    /// <summary>Display as Anonymous Supporter on the public portal while showing the amount.</summary>
    AnonymousPublicly = 2,

    /// <summary>Omit completely from public donor feeds (visible only to authorized staff/leaders).</summary>
    PrivateStaffOnly = 3,
}
