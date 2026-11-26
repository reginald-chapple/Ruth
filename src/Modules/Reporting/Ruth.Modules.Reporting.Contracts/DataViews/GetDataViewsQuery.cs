using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Lists DataViews optionally filtered by entity type key and active flag.</summary>
/// <param name="EntityTypeKey">Optional entity type key filter.</param>
/// <param name="IncludeInactive">Whether to include inactive DataViews.</param>
public sealed record GetDataViewsQuery(string? EntityTypeKey = null, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<DataViewResponse>>;
