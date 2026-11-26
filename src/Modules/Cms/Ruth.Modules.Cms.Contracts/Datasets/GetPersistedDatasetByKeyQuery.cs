using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Datasets;

/// <summary>Query to retrieve a persisted dataset by access key (refreshing on demand if expired).</summary>
public sealed record GetPersistedDatasetByKeyQuery(string AccessKey, bool RefreshIfStale = true)
    : IQuery<PersistedDatasetResponse>;
