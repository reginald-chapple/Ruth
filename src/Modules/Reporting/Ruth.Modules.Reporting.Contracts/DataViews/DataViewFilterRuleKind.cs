namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Kind of leaf predicate evaluated inside a DataView Query AST.</summary>
public enum DataViewFilterRuleKind
{
    /// <summary>Evaluates a direct scalar property on the target entity.</summary>
    EntityProperty = 0,

    /// <summary>Evaluates a typed key inside an entity jsonb attribute bag.</summary>
    JsonbAttribute = 1,

    /// <summary>Evaluates a PostGIS spatial radius (latitude, longitude, radiusMeters) filter.</summary>
    SpatialRadius = 2,

    /// <summary>Evaluates a PostGIS spatial polygon (WKT/GeoJSON polygon) filter.</summary>
    SpatialPolygon = 3,

    /// <summary>Intersects or excludes a cross-module PersonId set (DataView, Group, Finance, Attendance).</summary>
    CrossModulePersonSet = 4,
}
