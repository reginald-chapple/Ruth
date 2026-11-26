using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Lists embedded VirtualRooms filtered by optional context kind or active flag.</summary>
/// <param name="ContextKind">Optional domain context filter.</param>
/// <param name="IncludeInactive">When true, includes inactive rooms.</param>
public sealed record GetVirtualRoomsQuery(
    VirtualRoomContextKind? ContextKind = null,
    bool IncludeInactive = false
) : IQuery<IReadOnlyList<VirtualRoomResponse>>;
