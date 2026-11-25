namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Enforcement level for <see cref="Groups.GroupDetailResponse.GroupCapacity" />.</summary>
public enum GroupCapacityRule
{
    /// <summary>Capacity is informational only.</summary>
    None = 0,

    /// <summary>Capacity emits a warning when exceeded while still permitting addition.</summary>
    Soft = 1,

    /// <summary>Capacity blocks adding or activating members beyond the limit.</summary>
    Hard = 2,
}
