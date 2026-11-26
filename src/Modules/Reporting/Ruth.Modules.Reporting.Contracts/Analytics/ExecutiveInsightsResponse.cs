namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Executive Insights dashboard combining cross-module KPI cards and star-schema freshness.</summary>
/// <param name="GeneratedAtUtc">Timestamp when insights were generated.</param>
/// <param name="WindowDays">Lookback window in days.</param>
/// <param name="BranchId">Optional branch scope.</param>
/// <param name="Kpis">Executive KPI cards.</param>
/// <param name="StarSchemaLastRefreshedUtc">Last BI star-schema projection refresh timestamp.</param>
/// <param name="StarSchemaFactRowCount">Total projected rows across star-schema fact tables.</param>
public sealed record ExecutiveInsightsResponse(
    DateTimeOffset GeneratedAtUtc,
    int WindowDays,
    Guid? BranchId,
    IReadOnlyList<ExecutiveKpiCardDto> Kpis,
    DateTimeOffset? StarSchemaLastRefreshedUtc,
    int StarSchemaFactRowCount
);
