namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Named chart series inside a DynamicChart response.</summary>
/// <param name="SeriesName">Partition or metric series display label.</param>
/// <param name="PartitionEntityId">Optional partition entity identifier.</param>
/// <param name="Points">Ordered time-bucket points.</param>
public sealed record DynamicChartSeriesDto(
    string SeriesName,
    Guid? PartitionEntityId,
    IReadOnlyList<DynamicChartPointDto> Points
);
