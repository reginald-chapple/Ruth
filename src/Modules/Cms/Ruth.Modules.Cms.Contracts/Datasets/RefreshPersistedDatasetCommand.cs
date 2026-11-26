using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Datasets;

/// <summary>Command to force-rebuild a persisted dataset's cached JSON payload.</summary>
public sealed record RefreshPersistedDatasetCommand(string AccessKey)
    : ICommand<PersistedDatasetResponse>;
