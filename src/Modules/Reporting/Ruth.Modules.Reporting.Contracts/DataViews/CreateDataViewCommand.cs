using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Creates a new declarative DataView with its Query AST filter tree.</summary>
/// <param name="Name">Human-readable name.</param>
/// <param name="EntityTypeKey">Target entity type key (e.g., Ruth.Crm.Person).</param>
/// <param name="RootFilter">Root Query AST node.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional category identifier.</param>
/// <param name="TransformKey">Optional post-filter transform key.</param>
/// <param name="PersistedScheduleIntervalMinutes">Optional materialization cadence in minutes.</param>
public sealed record CreateDataViewCommand(
    string Name,
    string EntityTypeKey,
    DataViewFilterNodeDto RootFilter,
    string? Description = null,
    Guid? CategoryId = null,
    string? TransformKey = null,
    int? PersistedScheduleIntervalMinutes = null
) : ICommand<DataViewResponse>;
