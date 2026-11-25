using NetTopologySuite.Geometries;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>Association between a <see cref="Group" /> and a physical/geographic Location with PostGIS coordinates.</summary>
public sealed class GroupLocation : Entity<GroupLocationId>
{
    private List<Guid> _scheduleIds = [];

    private GroupLocation() { }

    private GroupLocation(GroupLocationId id, GroupId groupId, Guid locationId)
        : base(id)
    {
        GroupId = groupId;
        LocationId = locationId;
    }

    /// <summary>Gets the owning Group identifier.</summary>
    public GroupId GroupId { get; private set; }

    /// <summary>Gets the referenced Core Location identifier.</summary>
    public Guid LocationId { get; private set; }

    /// <summary>Gets the display label for the location.</summary>
    public string LocationName { get; private set; } = string.Empty;

    /// <summary>Gets the optional Core DefinedValue GroupLocationType identifier.</summary>
    public Guid? GroupLocationTypeValueId { get; private set; }

    /// <summary>Gets whether this is the mailing location.</summary>
    public bool IsMailingLocation { get; private set; }

    /// <summary>Gets whether this location is mappable in GroupFinder.</summary>
    public bool IsMappedLocation { get; private set; } = true;

    /// <summary>Gets whether this location serves as an overflow location for check-in.</summary>
    public bool IsOverflowLocation { get; private set; }

    /// <summary>Gets the optional GroupMember PersonId when derived from a member's home address.</summary>
    public Guid? GroupMemberPersonId { get; private set; }

    /// <summary>Gets the display sort order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets the PostGIS WGS84 point coordinate for radius search and map pins.</summary>
    public Point? GeoPoint { get; private set; }

    /// <summary>Gets the PostGIS WGS84 polygon geofence for area/neighborhood matching.</summary>
    public Polygon? GeoFence { get; private set; }

    /// <summary>Gets the associated Core Schedule identifiers stored as JSONB.</summary>
    internal List<Guid> ScheduleIdsList
    {
        get => _scheduleIds;
        set => _scheduleIds = value ?? [];
    }

    /// <summary>Gets the associated Core Schedule identifiers.</summary>
    public IReadOnlyList<Guid> ScheduleIds => _scheduleIds;

    internal static Result<GroupLocation> Create(
        GroupId groupId,
        Guid locationId,
        string? locationName,
        Guid? groupLocationTypeValueId,
        bool isMailingLocation,
        bool isMappedLocation,
        bool isOverflowLocation,
        Guid? groupMemberPersonId,
        int order,
        GroupGeoPointDto? geoPoint,
        IReadOnlyList<GroupGeoPointDto>? geoFenceRing,
        IReadOnlyList<Guid>? scheduleIds
    )
    {
        var location = new GroupLocation(GroupLocationId.Create(), groupId, locationId);
        var update = location.Update(
            locationName,
            groupLocationTypeValueId,
            isMailingLocation,
            isMappedLocation,
            isOverflowLocation,
            groupMemberPersonId,
            order,
            geoPoint,
            geoFenceRing,
            scheduleIds
        );
        return update.IsFailure
            ? Result.Failure<GroupLocation>(update.Errors)
            : Result.Success(location);
    }

    internal Result Update(
        string? locationName,
        Guid? groupLocationTypeValueId,
        bool isMailingLocation,
        bool isMappedLocation,
        bool isOverflowLocation,
        Guid? groupMemberPersonId,
        int order,
        GroupGeoPointDto? geoPoint,
        IReadOnlyList<GroupGeoPointDto>? geoFenceRing,
        IReadOnlyList<Guid>? scheduleIds
    )
    {
        var nameResult = GroupsTextPolicy.NormalizeOptional(
            locationName,
            GroupsTextPolicy.ShortLabelMaxLength,
            allowNewLines: false
        );
        var pointResult = GroupsGeoHelper.CreatePoint(geoPoint);
        var polygonResult = GroupsGeoHelper.CreatePolygon(geoFenceRing);
        var combined = Result.Combine(nameResult, pointResult, polygonResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        LocationName = nameResult.Value ?? "Location";
        GroupLocationTypeValueId = groupLocationTypeValueId;
        IsMailingLocation = isMailingLocation;
        IsMappedLocation = isMappedLocation;
        IsOverflowLocation = isOverflowLocation;
        GroupMemberPersonId = groupMemberPersonId;
        Order = order;
        GeoPoint = pointResult.Value;
        GeoFence = polygonResult.Value;
        _scheduleIds = scheduleIds is null ? [] : [.. scheduleIds.Distinct()];
        return Result.Success();
    }

    internal void RepointGroupMemberPerson(Guid survivingPersonId)
    {
        GroupMemberPersonId = survivingPersonId;
    }
}
