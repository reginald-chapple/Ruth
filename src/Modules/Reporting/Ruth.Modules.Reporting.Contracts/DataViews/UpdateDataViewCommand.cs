using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Updates an existing DataView definition and replaces its Query AST filter tree.</summary>
/// <param name="DataViewId">Target DataView identifier.</param>
/// <param name="Name">Updated name.</param>
/// <param name="RootFilter">Updated root Query AST node.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional category identifier.</param>
/// <param name="TransformKey">Optional transform key.</param>
/// <param name="PersistedScheduleIntervalMinutes">Optional materialization cadence in minutes.</param>
/// <param name="IsActive">Active status flag.</param>
public sealed record UpdateDataViewCommand(
    Guid DataViewId,
    string Name,
    DataViewFilterNodeDto RootFilter,
    string? Description = null,
    Guid? CategoryId = null,
    string? TransformKey = null,
    int? PersistedScheduleIntervalMinutes = null,
    bool IsActive = true
) : ICommand<DataViewResponse>;
