using Microsoft.AspNetCore.Components;
using Ruth.Host.Middleware;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Cms;

/// <summary>Dynamic CMS page renderer resolving Site -> Layout -> Page -> Zone -> Block via DynamicComponent.</summary>
public sealed partial class CmsPageHost : ComponentBase
{
    private ResolvedCmsPageResponse? _resolved;
    private string? _errorMessage;

    /// <summary>Gets or sets the optional preview site slug.</summary>
    [Parameter]
    public string? SiteSlug { get; set; }

    /// <summary>Gets or sets the catch-all CMS route path.</summary>
    [Parameter]
    public string? CmsPath { get; set; }

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private RuthBlockComponentRegistry BlockRegistry { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private string ResolvePageTitle()
    {
        if (_resolved is not null)
        {
            var pageTitle = string.IsNullOrWhiteSpace(_resolved.Page.BrowserTitle)
                ? _resolved.Page.PageTitle
                : _resolved.Page.BrowserTitle;
            return $"{pageTitle} — {_resolved.Site.Name}";
        }

        var brand = Services.GetService<TenantWorkspaceBrandingState>()?.BrandTitle
            ?? "Organization Workspace";
        return $"CMS Page — {brand}";
    }

    protected override async Task OnParametersSetAsync()
    {
        var uri = new Uri(Navigation.Uri);
        var res = await QueryDispatcher.QueryAsync<ResolveCmsPageQuery, ResolvedCmsPageResponse>(
            new ResolveCmsPageQuery(
                Host: uri.Host,
                SiteSlug: SiteSlug,
                Path: CmsPath,
                IncludeUnpublished: !string.IsNullOrWhiteSpace(SiteSlug)
            )
        );

        if (res.IsSuccess)
        {
            _resolved = res.Value;
            _errorMessage = null;
        }
        else
        {
            _resolved = null;
            _errorMessage = res.PrimaryError.Description;
        }
    }

    private Dictionary<string, object> BuildBlockParameters(
        CmsBlockResponse block,
        RuthBlockDescriptor descriptor
    )
    {
        var parameters = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Block"] = block,
        };

        if (
            string.Equals(
                descriptor.BlockKey,
                "cms.html-content",
                StringComparison.OrdinalIgnoreCase
            ) && _resolved is not null
        )
        {
            if (_resolved.ActiveHtmlByBlockId.TryGetValue(block.Id, out var html))
            {
                parameters["ActiveHtml"] = html;
            }

            parameters["RouteParameters"] = _resolved.RouteParameters;
        }

        return parameters;
    }
}
