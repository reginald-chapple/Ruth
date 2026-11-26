namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Read-only row returned from evaluating a DataView or Report.</summary>
/// <param name="EntityId">Primary entity identifier.</param>
/// <param name="EntityTypeKey">Entity type key of the row.</param>
/// <param name="PersonId">Optional associated constituent PersonId for cross-module set intersections.</param>
/// <param name="Values">Scalar field values keyed by field identifier.</param>
/// <param name="Attributes">Optional jsonb attribute key-value map.</param>
/// <param name="Latitude">Optional WGS84 latitude for spatial predicates.</param>
/// <param name="Longitude">Optional WGS84 longitude for spatial predicates.</param>
public sealed record DataViewEntityRecord(
    Guid EntityId,
    string EntityTypeKey,
    Guid? PersonId,
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyDictionary<string, string?>? Attributes = null,
    double? Latitude = null,
    double? Longitude = null
);
