using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="CommunicationRecipient" />.</summary>
public readonly record struct CommunicationRecipientId(Guid Value)
    : IStronglyTypedId<CommunicationRecipientId>
{
    public static CommunicationRecipientId Empty => new(Guid.Empty);

    public static CommunicationRecipientId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static CommunicationRecipientId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
