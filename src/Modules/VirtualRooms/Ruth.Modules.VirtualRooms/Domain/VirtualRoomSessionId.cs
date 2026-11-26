using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.VirtualRooms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a VirtualRoomSession.</summary>
public readonly record struct VirtualRoomSessionId(Guid Value)
    : IStronglyTypedId<VirtualRoomSessionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static VirtualRoomSessionId Empty => new(Guid.Empty);

    public static VirtualRoomSessionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static VirtualRoomSessionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
