namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Allowed location selection modes for groups of a given group type.</summary>
[Flags]
public enum GroupLocationPickerMode
{
    /// <summary>No location selection enabled.</summary>
    None = 0,

    /// <summary>Named building or room location.</summary>
    Named = 1,

    /// <summary>Street address location.</summary>
    Address = 2,

    /// <summary>Geographic WGS84 point coordinate.</summary>
    Point = 4,

    /// <summary>Geographic WGS84 polygon geofence.</summary>
    Polygon = 8,

    /// <summary>Derived from a group member's home address.</summary>
    GroupMember = 16,
}
