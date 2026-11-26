using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Campaigns;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="CampaignParticipant" />.</summary>
public readonly record struct CampaignParticipantId(Guid Value)
    : IStronglyTypedId<CampaignParticipantId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static CampaignParticipantId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static CampaignParticipantId Create()
    {
        return new CampaignParticipantId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static CampaignParticipantId From(Guid value)
    {
        return new CampaignParticipantId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
