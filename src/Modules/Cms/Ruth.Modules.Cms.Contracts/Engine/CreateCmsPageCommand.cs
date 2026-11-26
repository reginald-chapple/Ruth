using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to create a CMS page with optional initial routes and entity contexts.</summary>
public sealed record CreateCmsPageCommand(
    Guid SiteId,
    Guid LayoutId,
    string InternalName,
    string PageTitle,
    string? BrowserTitle = null,
    Guid? ParentPageId = null,
    string? Description = null,
    string? IconCssClass = null,
    int Order = 0,
    PageNavDisplayMode DisplayInNavWhen = PageNavDisplayMode.WhenAllowed,
    bool RequiresEncryption = false,
    int OutputCacheDurationSeconds = 0,
    bool IsPublished = true,
    bool SetAsSiteDefault = false,
    IReadOnlyList<string>? Routes = null,
    IReadOnlyDictionary<string, string>? Contexts = null
) : ICommand<CmsPageResponse>;
