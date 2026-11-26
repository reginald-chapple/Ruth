using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Command to add a deep link rule to a MobileApplication.</summary>
public sealed record AddMobileDeepLinkCommand(
    Guid MobileApplicationId,
    string RoutePrefix,
    string FallbackUrl,
    Guid? TargetMobilePageId = null,
    bool UsesUniversalLinks = true
) : ICommand<MobileApplicationResponse>;
