using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Closes a VirtualRoomSession, finalizes open participant dwell intervals, and records qualified Group Attendance.</summary>
/// <param name="SessionId">Target VirtualRoomSession identifier.</param>
public sealed record CloseVirtualRoomSessionCommand(Guid SessionId)
    : ICommand<VirtualRoomSessionResponse>;
