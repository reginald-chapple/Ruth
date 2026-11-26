namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Represents a synced media folder and its elements returned by a media provider.</summary>
public sealed record SyncedMediaFolderSpec(
    string ExternalFolderKey,
    string Name,
    string? Description,
    IReadOnlyList<SyncedMediaElementSpec> Elements
);
