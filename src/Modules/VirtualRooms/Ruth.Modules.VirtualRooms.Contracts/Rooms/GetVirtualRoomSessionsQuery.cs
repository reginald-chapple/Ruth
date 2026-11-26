using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Lists VirtualRoomSessions filtered by optional VirtualRoomId or status.</summary>
/// <param name="VirtualRoomId">Optional VirtualRoom filter.</param>
/// <param name="Status">Optional session status filter.</param>
public sealed record GetVirtualRoomSessionsQuery(
    Guid? VirtualRoomId = null,
    VirtualRoomSessionStatus? Status = null
) : IQuery<IReadOnlyList<VirtualRoomSessionResponse>>;
