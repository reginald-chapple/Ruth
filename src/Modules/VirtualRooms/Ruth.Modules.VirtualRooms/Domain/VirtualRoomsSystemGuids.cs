namespace Ruth.Modules.VirtualRooms.Domain;

/// <summary>Deterministic compile-time SystemGuids for VirtualRooms entity registrations.</summary>
public static class VirtualRoomsSystemGuids
{
    public static readonly Guid VirtualRoom = new(
        0x01966500,
        0x0007,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x05,
        0x07,
        0x01
    );

    public static readonly Guid VirtualRoomSession = new(
        0x01966500,
        0x0007,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x05,
        0x07,
        0x02
    );
}
