using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to create a CMS site with optional host domains.</summary>
public sealed record CreateCmsSiteCommand(
    string Name,
    string Slug,
    string? Description = null,
    Guid? ThemeId = null,
    Guid? BranchId = null,
    CmsSiteType SiteType = CmsSiteType.Web,
    bool EnablePageViews = true,
    IReadOnlyList<string>? Domains = null
) : ICommand<CmsSiteResponse>;
