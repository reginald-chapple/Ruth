using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Campaigns;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="CampaignContributionCredit" />.</summary>
public readonly record struct CampaignContributionCreditId(Guid Value)
    : IStronglyTypedId<CampaignContributionCreditId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static CampaignContributionCreditId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static CampaignContributionCreditId Create()
    {
        return new CampaignContributionCreditId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static CampaignContributionCreditId From(Guid value)
    {
        return new CampaignContributionCreditId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
