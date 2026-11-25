using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Relationships;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="PeerNetworkEdge" /> entity.</summary>
public readonly record struct PeerNetworkEdgeId(Guid Value) : IStronglyTypedId<PeerNetworkEdgeId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static PeerNetworkEdgeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 peer-network edge identifier.</summary>
    public static PeerNetworkEdgeId Create()
    {
        return new PeerNetworkEdgeId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static PeerNetworkEdgeId From(Guid value)
    {
        return new PeerNetworkEdgeId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
