using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Schedules or immediately starts a VirtualRoomSession occurrence.</summary>
/// <param name="VirtualRoomId">Target VirtualRoom identifier.</param>
/// <param name="Title">Session title.</param>
/// <param name="ScheduledStartUtc">Scheduled start UTC timestamp.</param>
/// <param name="ScheduledEndUtc">Scheduled end UTC timestamp.</param>
/// <param name="StartLiveNow">When true, provisions the LiveKit room immediately and transitions to Live.</param>
public sealed record CreateVirtualRoomSessionCommand(
    Guid VirtualRoomId,
    string Title,
    DateTimeOffset ScheduledStartUtc,
    DateTimeOffset ScheduledEndUtc,
    bool StartLiveNow = true
) : ICommand<VirtualRoomSessionResponse>;
