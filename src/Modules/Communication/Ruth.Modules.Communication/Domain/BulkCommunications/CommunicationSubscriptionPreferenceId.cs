using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="CommunicationSubscriptionPreference" />.</summary>
public readonly record struct CommunicationSubscriptionPreferenceId(Guid Value)
    : IStronglyTypedId<CommunicationSubscriptionPreferenceId>
{
    public static CommunicationSubscriptionPreferenceId Empty => new(Guid.Empty);

    public static CommunicationSubscriptionPreferenceId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static CommunicationSubscriptionPreferenceId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
