using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to create a CMS CSS custom-property theme bundle.</summary>
public sealed record CreateCmsThemeCommand(
    string Name,
    string Slug,
    string? Description = null,
    IReadOnlyDictionary<string, string>? CssCustomProperties = null,
    string? CustomCss = null,
    bool IsActive = true
) : ICommand<CmsThemeResponse>;
