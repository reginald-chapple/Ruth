namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Represents a screen/page inside a MobileApplication.</summary>
public sealed record MobilePageResponse(
    Guid Id,
    Guid MobileApplicationId,
    Guid? MobileLayoutId,
    string Title,
    string PageKey,
    string Route,
    string? IconName,
    int Order,
    bool ShowInNavigation,
    bool RequiresAuthentication,
    Guid? ContentChannelId,
    string? AdaptiveMessageKey,
    string BlocksJson,
    bool IsActive
);
