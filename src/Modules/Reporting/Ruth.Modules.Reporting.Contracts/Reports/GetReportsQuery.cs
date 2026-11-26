using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Lists Reports optionally filtered by entity type key.</summary>
/// <param name="EntityTypeKey">Optional entity type key filter.</param>
/// <param name="IncludeInactive">Whether to include inactive reports.</param>
public sealed record GetReportsQuery(string? EntityTypeKey = null, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<ReportResponse>>;
