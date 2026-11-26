using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Materializes a DataView's matched entity IDs into reporting.data_view_persisted_values.</summary>
/// <param name="DataViewId">DataView identifier to materialize.</param>
public sealed record MaterializeDataViewCommand(Guid DataViewId)
    : ICommand<DataViewExecutionResponse>;
