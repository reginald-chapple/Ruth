namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Result of executing a DataView Query AST.</summary>
/// <param name="DataViewId">Executed DataView identifier.</param>
/// <param name="EntityTypeKey">Entity type key of the result set.</param>
/// <param name="TotalMatchedCount">Total matched records before truncation.</param>
/// <param name="ExecutionMilliseconds">Query execution time in milliseconds.</param>
/// <param name="UsedPersistedValues">Whether pre-materialized persisted IDs were used.</param>
/// <param name="CompiledSqlPreview">Parameterized PostgreSQL predicate preview generated from the AST.</param>
/// <param name="EntityIds">Matched entity IDs.</param>
/// <param name="Records">Matched entity records.</param>
public sealed record DataViewExecutionResponse(
    Guid DataViewId,
    string EntityTypeKey,
    int TotalMatchedCount,
    int ExecutionMilliseconds,
    bool UsedPersistedValues,
    string CompiledSqlPreview,
    IReadOnlyList<Guid> EntityIds,
    IReadOnlyList<DataViewEntityRecord> Records
);
