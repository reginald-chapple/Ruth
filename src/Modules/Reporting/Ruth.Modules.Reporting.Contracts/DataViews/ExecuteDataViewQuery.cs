using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Executes a DataView Query AST and returns matched entity IDs and records.</summary>
/// <param name="DataViewId">DataView identifier to execute.</param>
/// <param name="MaxRows">Maximum rows to return.</param>
/// <param name="UsePersistedIfFresh">When true, uses persisted values if fresh within the schedule interval.</param>
public sealed record ExecuteDataViewQuery(
    Guid DataViewId,
    int MaxRows = 250,
    bool UsePersistedIfFresh = true
) : IQuery<DataViewExecutionResponse>;
