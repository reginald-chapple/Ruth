using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Datasets;

/// <summary>Command to create a CMS persisted dataset.</summary>
public sealed record CreatePersistedDatasetCommand(
    string AccessKey,
    string Name,
    PersistedDatasetSourceKind DataSourceKind,
    string? Description = null,
    IReadOnlyDictionary<string, string?>? SourceConfiguration = null,
    int RefreshIntervalMinutes = 60,
    DateTimeOffset? ExpireDateTimeUtc = null,
    int MemoryCacheDurationMinutes = 15,
    bool AllowManualRefresh = true,
    bool RefreshImmediately = true
) : ICommand<PersistedDatasetResponse>;
