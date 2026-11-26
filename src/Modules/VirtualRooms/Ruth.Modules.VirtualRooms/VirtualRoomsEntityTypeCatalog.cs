using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.VirtualRooms;

internal static class VirtualRoomsEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        new(
            VirtualRoomsSystemGuids.VirtualRoom,
            VirtualRoom.EntityTypeKeyName,
            nameof(VirtualRoom),
            "Virtual Room",
            "VirtualRooms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: true
        ),
        new(
            VirtualRoomsSystemGuids.VirtualRoomSession,
            VirtualRoomSession.EntityTypeKeyName,
            nameof(VirtualRoomSession),
            "Virtual Room Session",
            "VirtualRooms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
    ];

    public static IReadOnlyList<EntityTypeRegistration> GetAll()
    {
        return _all;
    }
}
