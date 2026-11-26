using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Contracts.Personalization;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Hydrated screen payload for a mobile application page including channel items and adaptive banner.</summary>
public sealed record HydratedMobilePageResponse(
    string ContractVersion,
    MobilePageResponse Page,
    MobileLayoutResponse? Layout,
    IReadOnlyList<ContentChannelItemResponse> ChannelItems,
    ResolvedAdaptiveMessageResponse? AdaptiveMessage
);
