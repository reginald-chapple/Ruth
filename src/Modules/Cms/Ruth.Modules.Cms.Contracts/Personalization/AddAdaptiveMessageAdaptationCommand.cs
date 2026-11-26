using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Command to add a targeted adaptation to an existing adaptive message.</summary>
public sealed record AddAdaptiveMessageAdaptationCommand(
    Guid AdaptiveMessageId,
    string Name,
    string Headline,
    string BodyMarkup,
    int Order = 0,
    IReadOnlyList<string>? SegmentKeys = null,
    DateTimeOffset? StartDateTimeUtc = null,
    DateTimeOffset? ExpireDateTimeUtc = null,
    int? ViewSaturationCount = null,
    int? ViewSaturationInDays = null,
    string? CallToActionText = null,
    string? CallToActionUrl = null,
    string? ImageUrl = null
) : ICommand<AdaptiveMessageResponse>;
