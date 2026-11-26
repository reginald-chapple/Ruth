using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.VirtualRooms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a VirtualRoomParticipantSession.</summary>
public readonly record struct VirtualRoomParticipantAttendanceId(Guid Value)
    : IStronglyTypedId<VirtualRoomParticipantAttendanceId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static VirtualRoomParticipantAttendanceId Empty => new(Guid.Empty);

    public static VirtualRoomParticipantAttendanceId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static VirtualRoomParticipantAttendanceId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
