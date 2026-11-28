using Microsoft.AspNetCore.Components;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Cms;

/// <summary>Staff W6-S1 studio managing CMS themes, sites, domains, pages, zone blocks, versioned HTML, and shortlinks.</summary>
public sealed partial class CmsSitesAndPagesStudio : ComponentBase
{
    private IReadOnlyList<CmsSiteResponse> _sites = [];
    private IReadOnlyList<CmsPageResponse> _pages = [];
    private IReadOnlyList<CmsBlockResponse> _blocks = [];
    private IReadOnlyList<PageShortLinkResponse> _shortLinks = [];

    private Guid? _selectedSiteId;
    private Guid? _selectedPageId;
    private Guid? _lastHtmlBlockId;
    private Guid? _latestThemeId;

    private string _themeName = "Community Modern";
    private string _themeSlug = "community-modern";
    private string _brandAccentColor = "#0F6CBD";
    private string _siteName = "Public Community Portal";
    private string _siteSlug = "community-portal";
    private string _siteDomain = "portal.example.org";
    private string _blockName = "Welcome Hero";
    private string _blockZone = "Hero";
    private string _blockTypeKey = "cms.html-content";
    private string _htmlMarkup =
        "<h2>Welcome to {{ BlockName }}</h2><p>Community member and supporter portal.</p>";
    private string _shortToken = "join";
    private string _shortTargetUrl = "/cms/preview/community-portal/";
    private string? _qrSvgPreview;
    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var sitesRes = await QueryDispatcher.QueryAsync<
            GetCmsSitesQuery,
            IReadOnlyList<CmsSiteResponse>
        >(new GetCmsSitesQuery(IncludeInactive: true));
        _sites = sitesRes.IsSuccess ? sitesRes.Value : [];

        if (_selectedSiteId is null && _sites.Count > 0)
        {
            await SelectSiteAsync(_sites[0].Id);
        }
        else if (_selectedSiteId is { } sid)
        {
            await SelectSiteAsync(sid);
        }
    }

    private async Task SelectSiteAsync(Guid siteId)
    {
        _selectedSiteId = siteId;
        var pagesRes = await QueryDispatcher.QueryAsync<
            GetCmsPagesQuery,
            IReadOnlyList<CmsPageResponse>
        >(new GetCmsPagesQuery(siteId));
        _pages = pagesRes.IsSuccess ? pagesRes.Value : [];
        _selectedPageId = _pages.Count > 0 ? _pages[0].Id : null;

        if (_selectedPageId is { } pid)
        {
            var blocksRes = await QueryDispatcher.QueryAsync<
                GetCmsBlocksQuery,
                IReadOnlyList<CmsBlockResponse>
            >(new GetCmsBlocksQuery(PageId: pid));
            _blocks = blocksRes.IsSuccess ? blocksRes.Value : [];
            _lastHtmlBlockId = _blocks
                .FirstOrDefault(static b =>
                    string.Equals(
                        b.BlockTypeKey,
                        "cms.html-content",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                ?.Id;
        }
        else
        {
            _blocks = [];
        }

        var linksRes = await QueryDispatcher.QueryAsync<
            GetPageShortLinksQuery,
            IReadOnlyList<PageShortLinkResponse>
        >(new GetPageShortLinksQuery(siteId));
        _shortLinks = linksRes.IsSuccess ? linksRes.Value : [];
    }

    private async Task CreateThemeAsync()
    {
        var (ok, message, themeId) = await CmsSitesStudioHelper.CreateThemeAsync(
            CommandDispatcher,
            _themeName,
            _themeSlug,
            _brandAccentColor
        );
        if (themeId is { } tid)
        {
            _latestThemeId = tid;
        }

        SetStatus(ok, message);
    }

    private async Task CreateSiteAsync()
    {
        var siteRes = await CommandDispatcher.SendAsync<CreateCmsSiteCommand, CmsSiteResponse>(
            new CreateCmsSiteCommand(
                _siteName,
                _siteSlug,
                ThemeId: _latestThemeId,
                Domains: [_siteDomain]
            )
        );
        if (siteRes.IsFailure)
        {
            SetStatus(ok: false, siteRes.PrimaryError.Description);
            return;
        }

        var layoutRes = await CommandDispatcher.SendAsync<
            CreateCmsLayoutCommand,
            CmsLayoutResponse
        >(new CreateCmsLayoutCommand(siteRes.Value.Id, "Standard Full Width", "full-width"));
        if (layoutRes.IsSuccess)
        {
            _ = await CommandDispatcher.SendAsync<CreateCmsPageCommand, CmsPageResponse>(
                new CreateCmsPageCommand(
                    siteRes.Value.Id,
                    layoutRes.Value.Id,
                    "Home",
                    siteRes.Value.Name,
                    SetAsSiteDefault: true,
                    Routes: [""]
                )
            );
        }

        SetStatus(
            ok: true,
            $"Provisioned site '{siteRes.Value.Name}' with default layout and home page."
        );
        await SelectSiteAsync(siteRes.Value.Id);
        await ReloadAsync();
    }

    private async Task AddBlockAsync()
    {
        if (_selectedPageId is not { } pageId)
        {
            SetStatus(ok: false, "Select or create a site with a page first.");
            return;
        }

        var res = await CommandDispatcher.SendAsync<CreateCmsBlockCommand, CmsBlockResponse>(
            new CreateCmsBlockCommand(
                _blockTypeKey,
                _blockZone,
                _blockName,
                PageId: pageId,
                Order: _blocks.Count
            )
        );
        if (res.IsSuccess)
        {
            _lastHtmlBlockId = res.Value.Id;
        }

        SetStatus(
            res.IsSuccess,
            res.IsSuccess
                ? $"Added block '{res.Value.Name}' to zone '{res.Value.Zone}'."
                : res.PrimaryError.Description
        );
        if (_selectedSiteId is { } sid)
        {
            await SelectSiteAsync(sid);
        }
    }

    private async Task SaveAndApproveHtmlAsync()
    {
        if (_lastHtmlBlockId is not { } blockId)
        {
            SetStatus(ok: false, "Add an HTML Content block first.");
            return;
        }

        var (ok, message) = await CmsSitesStudioHelper.PublishHtmlVersionAsync(
            CommandDispatcher,
            blockId,
            _htmlMarkup
        );
        SetStatus(ok, message);
    }

    private async Task CreateShortLinkAsync()
    {
        if (_selectedSiteId is not { } siteId)
        {
            return;
        }

        var (ok, message, qrSvg) = await CmsSitesStudioHelper.CreateShortLinkWithQrAsync(
            CommandDispatcher,
            QueryDispatcher,
            siteId,
            _shortToken,
            _shortTargetUrl
        );
        _qrSvgPreview = qrSvg;
        SetStatus(ok, message);
        await SelectSiteAsync(siteId);
    }

    private void SetStatus(bool ok, string message)
    {
        _isError = !ok;
        _statusMessage = message;
    }
}
