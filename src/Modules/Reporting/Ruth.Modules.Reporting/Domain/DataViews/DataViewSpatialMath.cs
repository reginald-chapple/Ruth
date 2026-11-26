using System.Globalization;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Domain.DataViews;

/// <summary>Clean-room WGS84 Haversine distance and ray-casting point-in-polygon geometry helpers.</summary>
public static class DataViewSpatialMath
{
    private const double EarthRadiusMeters = 6_371_008.8;

    public static Result ValidateRadiusRule(string? centerLatLon, string? radiusMetersText)
    {
        return !TryParseLatLon(centerLatLon, out _, out _)
                ? Result.Failure(
                    ReportingErrors.Validation(
                        "DataView.SpatialCenterInvalid",
                        "SpatialRadius Value must be formatted as 'latitude,longitude'."
                    )
                )
            : !double.TryParse(
                radiusMetersText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var radius
            )
            || radius <= 0
                ? Result.Failure(
                    ReportingErrors.Validation(
                        "DataView.SpatialRadiusInvalid",
                        "SpatialRadius SecondaryValue must be a positive radius in meters."
                    )
                )
            : Result.Success();
    }

    public static Result ValidatePolygonRule(string? polygonText)
    {
        return TryParsePolygon(polygonText, out var vertices) && vertices.Count >= 3
            ? Result.Success()
            : Result.Failure(
                ReportingErrors.Validation(
                    "DataView.SpatialPolygonInvalid",
                    "SpatialPolygon Value must contain at least 3 'lat,lon' vertices separated by ';'."
                )
            );
    }

    public static bool IsWithinRadius(
        double? latitude,
        double? longitude,
        string? centerLatLon,
        string? radiusMetersText
    )
    {
        return latitude is not null
            && longitude is not null
            && TryParseLatLon(centerLatLon, out var centerLat, out var centerLon)
            && double.TryParse(
                radiusMetersText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var radiusMeters
            )
            && HaversineMeters(latitude.Value, longitude.Value, centerLat, centerLon)
                <= radiusMeters;
    }

    public static bool IsInsidePolygon(double? latitude, double? longitude, string? polygonText)
    {
        if (
            latitude is null
            || longitude is null
            || !TryParsePolygon(polygonText, out var vertices)
            || vertices.Count < 3
        )
        {
            return false;
        }

        var inside = false;
        var lat = latitude.Value;
        var lon = longitude.Value;

        for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
        {
            var (latI, lonI) = vertices[i];
            var (latJ, lonJ) = vertices[j];
            var intersects =
                (lonI > lon) != (lonJ > lon)
                && lat < ((latJ - latI) * (lon - lonI) / (lonJ - lonI)) + latI;
            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var radLat1 = DegreesToRadians(lat1);
        var radLat2 = DegreesToRadians(lat2);

        var sinLat = Math.Sin(dLat / 2.0);
        var sinLon = Math.Sin(dLon / 2.0);
        var a = (sinLat * sinLat) + (Math.Cos(radLat1) * Math.Cos(radLat2) * sinLon * sinLon);
        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(Math.Max(0.0, 1.0 - a)));
        return EarthRadiusMeters * c;
    }

    public static bool TryParseLatLon(string? text, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length == 2
            && double.TryParse(
                parts[0],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out latitude
            )
            && double.TryParse(
                parts[1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out longitude
            )
            && latitude is >= -90 and <= 90
            && longitude is >= -180 and <= 180;
    }

    private static bool TryParsePolygon(string? text, out List<(double Lat, double Lon)> vertices)
    {
        vertices = [];
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var segments = text.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        foreach (var segment in segments)
        {
            if (!TryParseLatLon(segment, out var lat, out var lon))
            {
                vertices.Clear();
                return false;
            }

            vertices.Add((lat, lon));
        }

        return vertices.Count >= 3;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
