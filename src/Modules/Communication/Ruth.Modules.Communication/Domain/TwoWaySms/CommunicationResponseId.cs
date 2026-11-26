using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.TwoWaySms;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="CommunicationResponse" />.</summary>
public readonly record struct CommunicationResponseId(Guid Value)
    : IStronglyTypedId<CommunicationResponseId>
{
    public static CommunicationResponseId Empty => new(Guid.Empty);

    public static CommunicationResponseId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static CommunicationResponseId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
