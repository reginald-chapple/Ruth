using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Creates a Report backed by an entity type and optional DataView.</summary>
/// <param name="Name">Report name.</param>
/// <param name="EntityTypeKey">Entity type key.</param>
/// <param name="Fields">Ordered column fields.</param>
/// <param name="DataViewId">Optional DataView identifier.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional category identifier.</param>
/// <param name="FetchTop">Optional max row count.</param>
public sealed record CreateReportCommand(
    string Name,
    string EntityTypeKey,
    IReadOnlyList<ReportFieldSpec> Fields,
    Guid? DataViewId = null,
    string? Description = null,
    Guid? CategoryId = null,
    int? FetchTop = null
) : ICommand<ReportResponse>;
