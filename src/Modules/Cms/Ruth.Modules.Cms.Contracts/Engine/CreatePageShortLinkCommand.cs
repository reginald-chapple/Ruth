using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to create a CMS shortlink with optional UTM attribution.</summary>
public sealed record CreatePageShortLinkCommand(
    Guid SiteId,
    string Token,
    string TargetUrl,
    Guid? CategoryId = null,
    bool IsPinned = false,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null
) : ICommand<PageShortLinkResponse>;
