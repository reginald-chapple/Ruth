using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Creates a new embedded VirtualRoom.</summary>
/// <param name="Spec">VirtualRoom specification.</param>
public sealed record CreateVirtualRoomCommand(VirtualRoomSpec Spec) : ICommand<VirtualRoomResponse>;
