using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;

namespace Ruth.Modules.Groups.Slices.Groups;

internal static class GroupFinderProjector
{
    public static GroupFinderItemResponse? TryBuildMatchingItem(
        Group group,
        Dictionary<GroupTypeId, string> typeNames,
        Dictionary<GroupId, int> activeCounts,
        Dictionary<GroupId, Dictionary<string, string?>> demoByGroup,
        Dictionary<GroupId, GroupFinderSpatialMatch>? spatialMatches,
        FindGroupsQuery query
    )
    {
        var activeCount = activeCounts.GetValueOrDefault(group.Id, 0);
        var remaining = group.GroupCapacity.HasValue
            ? Math.Max(0, group.GroupCapacity.Value - activeCount)
            : (int?)null;
        var hasOpenCapacity = !group.GroupCapacity.HasValue || remaining > 0;
        if (query.HasOpenCapacityOnly && !hasOpenCapacity)
        {
            return null;
        }

        var groupDemographics = demoByGroup.GetValueOrDefault(group.Id)
            ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        return
            !MatchesFilterDictionary(group.AttributeValues, query.AttributeFilters)
            || !MatchesFilterDictionary(groupDemographics, query.DemographicFilters)
            ? null
            : BuildFinderItem(
                group,
                typeNames.GetValueOrDefault(group.GroupTypeId, "Group"),
                activeCount,
                remaining,
                hasOpenCapacity,
                spatialMatches,
                groupDemographics
            );
    }

    private static bool MatchesFilterDictionary(
        IReadOnlyDictionary<string, string?> source,
        IReadOnlyDictionary<string, string>? filters
    )
    {
        if (filters is null || filters.Count == 0)
        {
            return true;
        }

        foreach (var (key, expected) in filters)
        {
            if (
                !source.TryGetValue(key, out var actual)
                || !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
            )
            {
                return false;
            }
        }

        return true;
    }

    private static GroupFinderItemResponse BuildFinderItem(
        Group group,
        string groupTypeName,
        int activeCount,
        int? remaining,
        bool hasOpenCapacity,
        Dictionary<GroupId, GroupFinderSpatialMatch>? spatialMatches,
        Dictionary<string, string?> demographics
    )
    {
        var match = ResolveLocationMatch(group, spatialMatches);
        var (meters, miles) = RoundDistances(match.DistanceMeters);

        return new GroupFinderItemResponse(
            group.Id.Value,
            group.GroupTypeId.Value,
            groupTypeName,
            group.Name,
            group.Description,
            group.CampusId,
            group.ScheduleId,
            group.MeetingStyle,
            activeCount,
            group.GroupCapacity,
            remaining,
            hasOpenCapacity,
            match.LocationId,
            match.LocationName,
            match.GeoPoint,
            meters,
            miles,
            match.MatchedByGeofence,
            new Dictionary<string, string?>(
                group.AttributeValues,
                StringComparer.OrdinalIgnoreCase
            ),
            demographics
        );
    }

    private static GroupFinderSpatialMatch ResolveLocationMatch(
        Group group,
        Dictionary<GroupId, GroupFinderSpatialMatch>? spatialMatches
    )
    {
        if (spatialMatches is not null && spatialMatches.TryGetValue(group.Id, out var match))
        {
            return match;
        }

        var fallback = group
            .Locations.OrderBy(static l => l.Order)
            .FirstOrDefault(static l => l.IsMappedLocation);
        return new GroupFinderSpatialMatch(
            fallback?.LocationId,
            fallback?.LocationName,
            GroupsGeoHelper.ToPointDto(fallback?.GeoPoint),
            DistanceMeters: null,
            MatchedByGeofence: false
        );
    }

    private static (double? Meters, double? Miles) RoundDistances(double? rawMeters)
    {
        if (!rawMeters.HasValue)
        {
            return (null, null);
        }

        var meters = Math.Round(rawMeters.Value, 1, MidpointRounding.AwayFromZero);
        var miles = Math.Round(
            rawMeters.Value / GroupsGeoHelper.MetersPerMile,
            2,
            MidpointRounding.AwayFromZero
        );
        return (meters, miles);
    }
}
