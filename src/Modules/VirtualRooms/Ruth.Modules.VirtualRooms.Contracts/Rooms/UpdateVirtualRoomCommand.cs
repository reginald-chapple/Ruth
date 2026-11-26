using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Updates an existing embedded VirtualRoom.</summary>
/// <param name="RoomId">Target VirtualRoom identifier.</param>
/// <param name="Spec">Updated VirtualRoom specification.</param>
public sealed record UpdateVirtualRoomCommand(Guid RoomId, VirtualRoomSpec Spec)
    : ICommand<VirtualRoomResponse>;
