namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type RoomBalancingStrategy in the CheckIn module.</summary>
public enum RoomBalancingStrategy
{
    /// <summary>Fills rooms strictly by ascending priority order until soft/firm thresholds.</summary>
    OrderedPriority = 0,

    /// <summary>Assigns participants to the open room with the lowest active headcount.</summary>
    LeastOccupancyCount = 1,

    /// <summary>Assigns participants to the open room with the lowest occupancy-to-capacity ratio.</summary>
    LowestCapacityRatio = 2,
}
