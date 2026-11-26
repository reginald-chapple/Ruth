using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.VirtualRooms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a VirtualRoom.</summary>
public readonly record struct VirtualRoomId(Guid Value) : IStronglyTypedId<VirtualRoomId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static VirtualRoomId Empty => new(Guid.Empty);

    public static VirtualRoomId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static VirtualRoomId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
