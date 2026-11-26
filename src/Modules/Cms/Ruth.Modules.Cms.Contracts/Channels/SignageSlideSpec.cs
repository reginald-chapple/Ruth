namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Slide item displayed by a Digital Signage / TV application.</summary>
public sealed record SignageSlideSpec(
    string Title,
    string? Subtitle = null,
    string? ImageUrl = null,
    string? VideoUrl = null,
    int DurationSeconds = 15,
    int Order = 0,
    DateTimeOffset? StartDateTimeUtc = null,
    DateTimeOffset? ExpireDateTimeUtc = null
);
