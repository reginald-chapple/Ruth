using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Loads a VirtualRoom by UUID or slug.</summary>
/// <param name="IdOrSlug">VirtualRoom UUID or slug.</param>
public sealed record GetVirtualRoomByIdOrSlugQuery(string IdOrSlug) : IQuery<VirtualRoomResponse>;
