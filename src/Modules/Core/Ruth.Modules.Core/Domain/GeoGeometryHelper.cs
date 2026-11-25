using NetTopologySuite;
using NetTopologySuite.Geometries;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain;

internal static class GeoGeometryHelper
{
    public const int Wgs84Srid = 4326;

    private static readonly GeometryFactory _factory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: Wgs84Srid);

    public static Result<Point?> CreatePoint(GeoPointDto? dto)
    {
        return dto is null ? Result.Success<Point?>(value: null)
            : !IsValidCoordinate(dto.Latitude, dto.Longitude)
                ? (Result<Point?>)CoreLocationErrors.LocationGeoPointInvalid
            : Result.Success<Point?>(
                _factory.CreatePoint(new Coordinate(dto.Longitude, dto.Latitude))
            );
    }

    public static Result<Polygon?> CreatePolygon(IReadOnlyList<GeoPointDto>? ring)
    {
        if (ring is null || ring.Count == 0)
        {
            return Result.Success<Polygon?>(value: null);
        }

        if (ring.Count < 3)
        {
            return CoreLocationErrors.LocationGeoFenceInvalid;
        }

        var coordinates = new List<Coordinate>(ring.Count + 1);
        foreach (var point in ring)
        {
            if (!IsValidCoordinate(point.Latitude, point.Longitude))
            {
                return CoreLocationErrors.LocationGeoFenceInvalid;
            }

            coordinates.Add(new Coordinate(point.Longitude, point.Latitude));
        }

        if (!coordinates[0].Equals2D(coordinates[^1]))
        {
            coordinates.Add(coordinates[0].Copy());
        }

        if (coordinates.Count < 4)
        {
            return CoreLocationErrors.LocationGeoFenceInvalid;
        }

        var linearRing = _factory.CreateLinearRing([.. coordinates]);
        if (!linearRing.IsClosed || !linearRing.IsValid)
        {
            return CoreLocationErrors.LocationGeoFenceInvalid;
        }

        var polygon = _factory.CreatePolygon(linearRing);
        return !polygon.IsValid
            ? (Result<Polygon?>)CoreLocationErrors.LocationGeoFenceInvalid
            : Result.Success<Polygon?>(polygon);
    }

    public static GeoPointDto? ToPointDto(Point? point)
    {
        return point is null ? null : new GeoPointDto(point.Y, point.X);
    }

    public static IReadOnlyList<GeoPointDto>? ToRingDto(Polygon? polygon)
    {
        return polygon is null || polygon.IsEmpty
            ? null
            : [.. polygon.ExteriorRing.Coordinates.Select(static c => new GeoPointDto(c.Y, c.X))];
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
