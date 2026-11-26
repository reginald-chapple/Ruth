using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.Flows;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="CommunicationFlowInstanceRecipient" />.</summary>
public readonly record struct CommunicationFlowInstanceRecipientId(Guid Value)
    : IStronglyTypedId<CommunicationFlowInstanceRecipientId>
{
    public static CommunicationFlowInstanceRecipientId Empty => new(Guid.Empty);

    public static CommunicationFlowInstanceRecipientId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static CommunicationFlowInstanceRecipientId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
