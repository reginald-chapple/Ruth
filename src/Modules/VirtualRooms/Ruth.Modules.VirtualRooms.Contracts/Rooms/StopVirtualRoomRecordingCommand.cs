using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Stops an active recording egress on a VirtualRoomSession.</summary>
/// <param name="SessionId">Owning VirtualRoomSession identifier.</param>
/// <param name="EgressId">Target LiveKit egress identifier.</param>
public sealed record StopVirtualRoomRecordingCommand(Guid SessionId, string EgressId)
    : ICommand<VirtualRoomRecordingArchiveResponse>;
