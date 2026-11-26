namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Represents a synced media element fetched from a modular media account provider.</summary>
public sealed record SyncedMediaElementSpec(
    string ExternalElementKey,
    string Name,
    string? Description,
    int DurationSeconds,
    string? ThumbnailUrl,
    string? PlaybackUrl,
    string? HlsManifestUrl,
    string? CaptionsUrl,
    int? Width,
    int? Height,
    DateTimeOffset? PublishedDateTimeUtc,
    IReadOnlyDictionary<string, string?>? Metadata = null
);
