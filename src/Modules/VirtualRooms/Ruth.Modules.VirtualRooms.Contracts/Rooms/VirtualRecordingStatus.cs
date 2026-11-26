namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Lifecycle state of a RustFS S3 recording egress archive.</summary>
public enum VirtualRecordingStatus
{
    /// <summary>Egress job requested and initializing.</summary>
    Starting = 0,

    /// <summary>Actively streaming composite recording.</summary>
    Active = 1,

    /// <summary>Archived to RustFS S3 and ready for playback/transcript indexing.</summary>
    Completed = 2,

    /// <summary>Egress terminated with an error.</summary>
    Failed = 3,
}
