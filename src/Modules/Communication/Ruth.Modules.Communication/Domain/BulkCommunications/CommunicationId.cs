using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Communication" />.</summary>
public readonly record struct CommunicationId(Guid Value) : IStronglyTypedId<CommunicationId>
{
    public static CommunicationId Empty => new(Guid.Empty);

    public static CommunicationId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static CommunicationId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
