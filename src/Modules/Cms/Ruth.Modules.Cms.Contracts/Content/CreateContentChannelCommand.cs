using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Command to create a CMS content channel.</summary>
public sealed record CreateContentChannelCommand(
    Guid ContentChannelTypeId,
    string Name,
    string Slug,
    string? Description = null,
    string? IconCssClass = null,
    bool RequiresApproval = false,
    bool EnableRss = true,
    string? ChannelUrl = null,
    string? ItemUrl = null,
    int TimeToLiveMinutes = 60,
    bool ItemsManuallyOrdered = false,
    bool IsStructuredContent = false
) : ICommand<ContentChannelResponse>;
