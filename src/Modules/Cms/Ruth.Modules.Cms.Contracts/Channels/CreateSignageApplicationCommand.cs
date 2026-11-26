using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Command to create a Digital Signage, Apple TV, or Roku feed application.</summary>
public sealed record CreateSignageApplicationCommand(
    string Name,
    string Slug,
    SignagePlatformType Platform = SignagePlatformType.DigitalSignage,
    Guid? SiteId = null,
    int RefreshIntervalSeconds = 60,
    Guid? ThemeId = null,
    Guid? DefaultContentChannelId = null,
    Guid? MediaFolderId = null,
    string? AdaptiveMessageKey = null,
    IReadOnlyList<SignageSlideSpec>? Slides = null
) : ICommand<SignageApplicationResponse>;
