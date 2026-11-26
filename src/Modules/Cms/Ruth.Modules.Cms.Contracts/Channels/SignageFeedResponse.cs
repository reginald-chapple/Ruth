namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Rendered feed payload for Digital Signage, Apple TV, or Roku clients.</summary>
public sealed record SignageFeedResponse(
    Guid ApplicationId,
    string Slug,
    SignagePlatformType Platform,
    string Format,
    string ContentType,
    int RefreshIntervalSeconds,
    string PayloadJson,
    int ItemCount,
    DateTimeOffset GeneratedAtUtc
);
