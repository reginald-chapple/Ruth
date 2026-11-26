using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Command to add a page/screen to a MobileApplication.</summary>
public sealed record AddMobilePageCommand(
    Guid MobileApplicationId,
    string Title,
    string PageKey,
    string Route,
    Guid? MobileLayoutId = null,
    string? IconName = null,
    int Order = 0,
    bool ShowInNavigation = true,
    bool RequiresAuthentication = false,
    Guid? ContentChannelId = null,
    string? AdaptiveMessageKey = null,
    string BlocksJson = "[]"
) : ICommand<MobileApplicationResponse>;
