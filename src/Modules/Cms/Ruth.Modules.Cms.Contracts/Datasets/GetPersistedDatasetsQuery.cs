using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Datasets;

/// <summary>Query to list configured CMS persisted datasets.</summary>
public sealed record GetPersistedDatasetsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<PersistedDatasetResponse>>;
