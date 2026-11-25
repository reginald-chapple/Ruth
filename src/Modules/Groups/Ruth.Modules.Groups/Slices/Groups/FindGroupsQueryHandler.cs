using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class FindGroupsQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<FindGroupsQuery, IReadOnlyList<GroupFinderItemResponse>>
{
    public async Task<Result<IReadOnlyList<GroupFinderItemResponse>>> HandleAsync(
        FindGroupsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var spatialValidation = ValidateSpatialParameters(query);
        if (spatialValidation.IsFailure)
        {
            return Result.Failure<IReadOnlyList<GroupFinderItemResponse>>(spatialValidation.Errors);
        }

        var (searchPoint, searchPolygon) = spatialValidation.Value;
        var spatialMatches = await QuerySpatialMatchesAsync(
            searchPoint,
            query.RadiusMeters,
            searchPolygon,
            cancellationToken
        );

        var groups = await LoadCandidateGroupsAsync(query, spatialMatches, cancellationToken);
        return groups.Count == 0
            ? Result.Success<IReadOnlyList<GroupFinderItemResponse>>([])
            : Result.Success(
                await ProjectAndFilterResultsAsync(groups, spatialMatches, query, cancellationToken)
            );
    }

    private static Result<(Point? Point, Polygon? Polygon)> ValidateSpatialParameters(
        FindGroupsQuery query
    )
    {
        Point? point = null;
        if (query.Latitude.HasValue || query.Longitude.HasValue)
        {
            if (!query.Latitude.HasValue || !query.Longitude.HasValue)
            {
                return GroupsErrors.InvalidGeoPoint;
            }

            if (query.RadiusMeters is <= 0)
            {
                return GroupsErrors.InvalidSearchRadius;
            }

            var ptResult = GroupsGeoHelper.CreatePoint(
                new GroupGeoPointDto(query.Latitude.Value, query.Longitude.Value)
            );
            if (ptResult.IsFailure)
            {
                return Result.Failure<(Point?, Polygon?)>(ptResult.Errors);
            }

            point = ptResult.Value;
        }

        var polyResult = GroupsGeoHelper.CreatePolygon(query.PolygonRing);
        return polyResult.IsFailure
            ? Result.Failure<(Point?, Polygon?)>(polyResult.Errors)
            : Result.Success((point, polyResult.Value));
    }

    private async Task<Dictionary<GroupId, GroupFinderSpatialMatch>?> QuerySpatialMatchesAsync(
        Point? searchPoint,
        double? radiusMeters,
        Polygon? searchPolygon,
        CancellationToken cancellationToken
    )
    {
        if (searchPoint is null && searchPolygon is null)
        {
            return null;
        }

        var locQuery = ApplySpatialPredicates(
            dbContext.GroupLocations.AsNoTracking().Where(static gl => gl.IsMappedLocation),
            searchPoint,
            radiusMeters,
            searchPolygon
        );

        var projected = await locQuery
            .Select(gl => new SpatialRowProjection(
                gl.GroupId,
                gl.LocationId,
                gl.LocationName,
                gl.GeoPoint,
                searchPoint != null && gl.GeoPoint != null
                    ? gl.GeoPoint.Distance(searchPoint)
                    : null,
                searchPoint != null && gl.GeoFence != null && gl.GeoFence.Covers(searchPoint)
            ))
            .ToListAsync(cancellationToken);

        return SelectClosestPerGroup(projected);
    }

    private static IQueryable<GroupLocation> ApplySpatialPredicates(
        IQueryable<GroupLocation> locQuery,
        Point? searchPoint,
        double? radiusMeters,
        Polygon? searchPolygon
    )
    {
        if (searchPoint is not null)
        {
            var effectiveRadius = radiusMeters ?? 40_000.0;
            locQuery = locQuery.Where(gl =>
                (gl.GeoPoint != null && gl.GeoPoint.IsWithinDistance(searchPoint, effectiveRadius))
                || (gl.GeoFence != null && gl.GeoFence.Covers(searchPoint))
            );
        }

        if (searchPolygon is not null)
        {
            locQuery = locQuery.Where(gl =>
                gl.GeoPoint != null && searchPolygon.Covers(gl.GeoPoint)
            );
        }

        return locQuery;
    }

    private static Dictionary<GroupId, GroupFinderSpatialMatch> SelectClosestPerGroup(
        List<SpatialRowProjection> projected
    )
    {
        var bestByGroup = new Dictionary<GroupId, GroupFinderSpatialMatch>();
        foreach (var row in projected)
        {
            var distance = row.DistanceMeters ?? (row.MatchedByGeofence ? 0.0 : double.MaxValue);
            var existingDistance = bestByGroup.TryGetValue(row.GroupId, out var existing)
                ? existing.DistanceMeters ?? double.MaxValue
                : double.MaxValue;
            if (!bestByGroup.ContainsKey(row.GroupId) || distance < existingDistance)
            {
                bestByGroup[row.GroupId] = new GroupFinderSpatialMatch(
                    row.LocationId,
                    row.LocationName,
                    GroupsGeoHelper.ToPointDto(row.GeoPoint),
                    row.DistanceMeters,
                    row.MatchedByGeofence
                );
            }
        }

        return bestByGroup;
    }

    private async Task<List<Group>> LoadCandidateGroupsAsync(
        FindGroupsQuery query,
        Dictionary<GroupId, GroupFinderSpatialMatch>? spatialMatches,
        CancellationToken cancellationToken
    )
    {
        if (spatialMatches is { Count: 0 })
        {
            return [];
        }

        var groupsQuery = ApplyGroupFilters(
            dbContext
                .Groups.AsNoTracking()
                .Include(static g => g.Locations)
                .Where(static g => !g.IsArchived),
            query,
            spatialMatches
        );
        return await groupsQuery.ToListAsync(cancellationToken);
    }

    private static IQueryable<Group> ApplyGroupFilters(
        IQueryable<Group> groupsQuery,
        FindGroupsQuery query,
        Dictionary<GroupId, GroupFinderSpatialMatch>? spatialMatches
    )
    {
        if (query.PublicOnly)
        {
            groupsQuery = groupsQuery.Where(static g => g.IsPublic);
        }

        if (query.ActiveOnly)
        {
            groupsQuery = groupsQuery.Where(static g => g.IsActive);
        }

        if (query.GroupTypeId.HasValue)
        {
            var typeId = GroupTypeId.From(query.GroupTypeId.Value);
            groupsQuery = groupsQuery.Where(g => g.GroupTypeId == typeId);
        }

        if (query.BranchId.HasValue)
        {
            groupsQuery = groupsQuery.Where(g => g.BranchId == query.BranchId.Value);
        }

        if (query.MeetingStyle.HasValue)
        {
            groupsQuery = groupsQuery.Where(g => g.MeetingStyle == query.MeetingStyle.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var pattern = $"%{query.SearchText.Trim()}%";
            groupsQuery = groupsQuery.Where(g =>
                EF.Functions.ILike(g.Name, pattern)
                || (g.Description != null && EF.Functions.ILike(g.Description, pattern))
            );
        }

        if (spatialMatches is not null)
        {
            GroupId[] matchingIds = [.. spatialMatches.Keys];
            groupsQuery = groupsQuery.Where(g => matchingIds.Contains(g.Id));
        }

        return groupsQuery;
    }

    private async Task<IReadOnlyList<GroupFinderItemResponse>> ProjectAndFilterResultsAsync(
        List<Group> groups,
        Dictionary<GroupId, GroupFinderSpatialMatch>? spatialMatches,
        FindGroupsQuery query,
        CancellationToken cancellationToken
    )
    {
        GroupId[] groupIds = [.. groups.Select(static g => g.Id)];
        var typeNames = await dbContext
            .GroupTypes.AsNoTracking()
            .ToDictionaryAsync(static gt => gt.Id, static gt => gt.Name, cancellationToken);
        var activeCounts = await LoadActiveCountsAsync(groupIds, cancellationToken);
        var demoByGroup = await LoadDemographicsByGroupAsync(groupIds, cancellationToken);

        var items = new List<GroupFinderItemResponse>(groups.Count);
        foreach (var group in groups)
        {
            var item = GroupFinderProjector.TryBuildMatchingItem(
                group,
                typeNames,
                activeCounts,
                demoByGroup,
                spatialMatches,
                query
            );
            if (item is not null)
            {
                items.Add(item);
            }
        }

        var max = Math.Clamp(query.MaxResults, 1, 250);
        return
        [
            .. items
                .OrderBy(static i => i.DistanceMeters ?? double.MaxValue)
                .ThenBy(static i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Take(max),
        ];
    }

    private async Task<Dictionary<GroupId, int>> LoadActiveCountsAsync(
        GroupId[] groupIds,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .GroupMembers.AsNoTracking()
            .Where(m =>
                groupIds.Contains(m.GroupId)
                && !m.IsArchived
                && m.Status == GroupMemberStatus.Active
            )
            .GroupBy(static m => m.GroupId)
            .Select(static g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(static x => x.GroupId, static x => x.Count, cancellationToken);
    }

    private async Task<
        Dictionary<GroupId, Dictionary<string, string?>>
    > LoadDemographicsByGroupAsync(GroupId[] groupIds, CancellationToken cancellationToken)
    {
        var typeNames = await dbContext
            .GroupDemographicTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, static t => t.Name, cancellationToken);
        var rows = await dbContext
            .GroupDemographicValues.AsNoTracking()
            .Where(v => groupIds.Contains(v.GroupId))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<GroupId, Dictionary<string, string?>>();
        foreach (var row in rows)
        {
            if (!result.TryGetValue(row.GroupId, out var dict))
            {
                dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                result[row.GroupId] = dict;
            }

            if (typeNames.TryGetValue(row.GroupDemographicTypeId, out var name))
            {
                dict[name] = row.Value;
            }
        }

        return result;
    }

    private sealed record SpatialRowProjection(
        GroupId GroupId,
        Guid LocationId,
        string LocationName,
        Point? GeoPoint,
        double? DistanceMeters,
        bool MatchedByGeofence
    );
}
