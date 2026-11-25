namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Attached physical or geographic location on a group.</summary>
public sealed record GroupLocationResponse(
    Guid Id,
    Guid GroupId,
    Guid LocationId,
    string LocationName,
    Guid? GroupLocationTypeValueId,
    bool IsMailingLocation,
    bool IsMappedLocation,
    bool IsOverflowLocation,
    Guid? GroupMemberPersonId,
    int Order,
    GroupGeoPointDto? GeoPoint,
    IReadOnlyList<GroupGeoPointDto>? GeoFenceRing,
    IReadOnlyList<Guid> ScheduleIds
);
