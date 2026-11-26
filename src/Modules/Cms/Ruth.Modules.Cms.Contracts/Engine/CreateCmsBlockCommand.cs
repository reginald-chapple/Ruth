using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to place a configured CMS block into a Page, Layout, or Site zone.</summary>
public sealed record CreateCmsBlockCommand(
    string BlockTypeKey,
    string Zone,
    string Name,
    Guid? PageId = null,
    Guid? LayoutId = null,
    Guid? SiteId = null,
    int Order = 0,
    string? PreHtml = null,
    string? PostHtml = null,
    string? CssClass = null,
    int OutputCacheDurationSeconds = 0,
    IReadOnlyDictionary<string, string?>? Settings = null
) : ICommand<CmsBlockResponse>;
