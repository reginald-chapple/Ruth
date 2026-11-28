using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.Transports;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="CommunicationTransportConfig" />.</summary>
public readonly record struct CommunicationTransportConfigId(Guid Value)
    : IStronglyTypedId<CommunicationTransportConfigId>
{
    /// <summary>Gets the empty identifier.</summary>
    public static CommunicationTransportConfigId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static CommunicationTransportConfigId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Creates an identifier from an existing GUID.</summary>
    public static CommunicationTransportConfigId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
