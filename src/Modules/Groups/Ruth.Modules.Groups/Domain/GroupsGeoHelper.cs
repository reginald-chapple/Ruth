using NetTopologySuite;
using NetTopologySuite.Geometries;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain;

internal static class GroupsGeoHelper
{
    public const int Wgs84Srid = 4326;
    public const double MetersPerMile = 1609.344;

    private static readonly GeometryFactory _factory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: Wgs84Srid);

    public static Result<Point?> CreatePoint(GroupGeoPointDto? dto)
    {
        return dto is null ? Result.Success<Point?>(value: null)
            : !IsValidCoordinate(dto.Latitude, dto.Longitude)
                ? (Result<Point?>)GroupsErrors.InvalidGeoPoint
            : Result.Success<Point?>(
                _factory.CreatePoint(new Coordinate(dto.Longitude, dto.Latitude))
            );
    }

    public static Result<Polygon?> CreatePolygon(IReadOnlyList<GroupGeoPointDto>? ring)
    {
        if (ring is null || ring.Count == 0)
        {
            return Result.Success<Polygon?>(value: null);
        }

        if (ring.Count < 3)
        {
            return GroupsErrors.InvalidGeoPolygon;
        }

        var coordinates = new List<Coordinate>(ring.Count + 1);
        foreach (var point in ring)
        {
            if (!IsValidCoordinate(point.Latitude, point.Longitude))
            {
                return GroupsErrors.InvalidGeoPolygon;
            }

            coordinates.Add(new Coordinate(point.Longitude, point.Latitude));
        }

        if (!coordinates[0].Equals2D(coordinates[^1]))
        {
            coordinates.Add(coordinates[0].Copy());
        }

        if (coordinates.Count < 4)
        {
            return GroupsErrors.InvalidGeoPolygon;
        }

        var linearRing = _factory.CreateLinearRing([.. coordinates]);
        if (!linearRing.IsClosed || !linearRing.IsValid)
        {
            return GroupsErrors.InvalidGeoPolygon;
        }

        var polygon = _factory.CreatePolygon(linearRing);
        return !polygon.IsValid
            ? (Result<Polygon?>)GroupsErrors.InvalidGeoPolygon
            : Result.Success<Polygon?>(polygon);
    }

    public static GroupGeoPointDto? ToPointDto(Point? point)
    {
        return point is null ? null : new GroupGeoPointDto(point.Y, point.X);
    }

    public static IReadOnlyList<GroupGeoPointDto>? ToRingDto(Polygon? polygon)
    {
        return polygon is null || polygon.IsEmpty
            ? null
            :
            [
                .. polygon.ExteriorRing.Coordinates.Select(static c => new GroupGeoPointDto(
                    c.Y,
                    c.X
                )),
            ];
    }

    public static bool IsValidCoordinate(double latitude, double longitude)
    {
        return !double.IsNaN(latitude)
            && !double.IsInfinity(latitude)
            && !double.IsNaN(longitude)
            && !double.IsInfinity(longitude)
            && latitude is >= -90.0 and <= 90.0
            && longitude is >= -180.0 and <= 180.0;
    }
}
