namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Lifecycle status of a VirtualRoomSession occurrence.</summary>
public enum VirtualRoomSessionStatus
{
    /// <summary>Scheduled for a future window.</summary>
    Scheduled = 0,

    /// <summary>Currently live with active media room.</summary>
    Live = 1,

    /// <summary>Completed and closed.</summary>
    Ended = 2,

    /// <summary>Cancelled prior to completion.</summary>
    Cancelled = 3,
}
