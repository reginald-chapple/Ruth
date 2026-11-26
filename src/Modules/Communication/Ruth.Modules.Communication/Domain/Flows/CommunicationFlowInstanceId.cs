using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.Flows;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="CommunicationFlowInstance" />.</summary>
public readonly record struct CommunicationFlowInstanceId(Guid Value)
    : IStronglyTypedId<CommunicationFlowInstanceId>
{
    public static CommunicationFlowInstanceId Empty => new(Guid.Empty);

    public static CommunicationFlowInstanceId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static CommunicationFlowInstanceId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
