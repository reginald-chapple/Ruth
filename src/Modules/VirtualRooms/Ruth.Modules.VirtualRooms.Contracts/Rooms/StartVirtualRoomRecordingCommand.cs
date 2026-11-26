using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Starts a RustFS S3 composite MP4 recording egress on an active VirtualRoomSession.</summary>
/// <param name="SessionId">Target VirtualRoomSession identifier.</param>
/// <param name="Layout">Composite layout (grid or speaker).</param>
public sealed record StartVirtualRoomRecordingCommand(Guid SessionId, string Layout = "grid")
    : ICommand<VirtualRoomRecordingArchiveResponse>;
