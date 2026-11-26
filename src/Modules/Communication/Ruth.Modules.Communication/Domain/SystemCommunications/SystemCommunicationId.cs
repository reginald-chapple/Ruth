using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.SystemCommunications;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="SystemCommunication" />.</summary>
public readonly record struct SystemCommunicationId(Guid Value)
    : IStronglyTypedId<SystemCommunicationId>
{
    public static SystemCommunicationId Empty => new(Guid.Empty);

    public static SystemCommunicationId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static SystemCommunicationId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
