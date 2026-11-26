namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Represents a configured Digital Signage, Apple TV, or Roku feed application.</summary>
public sealed record SignageApplicationResponse(
    Guid Id,
    Guid? SiteId,
    string Name,
    string Slug,
    SignagePlatformType Platform,
    int RefreshIntervalSeconds,
    Guid? ThemeId,
    Guid? DefaultContentChannelId,
    Guid? MediaFolderId,
    string? AdaptiveMessageKey,
    IReadOnlyList<SignageSlideSpec> Slides,
    bool IsActive
);
