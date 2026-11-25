namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type RoomThresholdStatus in the CheckIn module.</summary>
public enum RoomThresholdStatus
{
    /// <summary>Open and below soft and firm capacity limits.</summary>
    Open = 0,

    /// <summary>At or above soft capacity while still below firm capacity.</summary>
    AtSoftCapacity = 1,

    /// <summary>At or above firm capacity; closed to automatic check-in assignments.</summary>
    AtFirmCapacity = 2,

    /// <summary>Manually closed or inactive for the requested schedule.</summary>
    Closed = 3,
}
