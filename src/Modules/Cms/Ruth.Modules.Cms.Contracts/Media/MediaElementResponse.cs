namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Represents a synchronized video/audio/HLS media asset.</summary>
public sealed record MediaElementResponse(
    Guid Id,
    Guid MediaFolderId,
    string Name,
    string ExternalElementKey,
    string? Description,
    int DurationSeconds,
    string? ThumbnailUrl,
    string? PlaybackUrl,
    string? HlsManifestUrl,
    string? CaptionsUrl,
    int? Width,
    int? Height,
    DateTimeOffset? PublishedDateTimeUtc,
    IReadOnlyDictionary<string, string?> Metadata
);
