using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Campaigns;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="FundraisingCampaign" />.</summary>
public readonly record struct FundraisingCampaignId(Guid Value)
    : IStronglyTypedId<FundraisingCampaignId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static FundraisingCampaignId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static FundraisingCampaignId Create()
    {
        return new FundraisingCampaignId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static FundraisingCampaignId From(Guid value)
    {
        return new FundraisingCampaignId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
