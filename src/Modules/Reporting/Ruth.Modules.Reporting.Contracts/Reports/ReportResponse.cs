namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Persisted Report definition with ordered ReportFields.</summary>
/// <param name="Id">Report identifier.</param>
/// <param name="Name">Report name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="EntityTypeKey">Entity type key.</param>
/// <param name="DataViewId">Optional backing DataView identifier.</param>
/// <param name="CategoryId">Optional category identifier.</param>
/// <param name="FetchTop">Optional row limit.</param>
/// <param name="IsActive">Active status.</param>
/// <param name="Fields">Ordered column fields.</param>
public sealed record ReportResponse(
    Guid Id,
    string Name,
    string? Description,
    string EntityTypeKey,
    Guid? DataViewId,
    Guid? CategoryId,
    int? FetchTop,
    bool IsActive,
    IReadOnlyList<ReportFieldResponse> Fields
);
