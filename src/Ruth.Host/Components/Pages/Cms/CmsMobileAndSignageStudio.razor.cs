using Microsoft.AspNetCore.Components;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Cms;

/// <summary>Staff W6-S4 studio managing MobileApplication shells and Digital Signage / Apple TV / Roku feeds.</summary>
public sealed partial class CmsMobileAndSignageStudio : ComponentBase
{
    private IReadOnlyList<MobileApplicationResponse> _mobileApps = [];
    private IReadOnlyList<SignageApplicationResponse> _signageApps = [];

    private string _mobileName = "Member Companion Mobile";
    private string _mobileSlug = "member-companion";
    private string _bundleId = "org.ruth.companion";
    private string _signageName = "Lobby Display & TV";
    private string _signageSlug = "lobby-display";
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
        var mobRes = await QueryDispatcher.QueryAsync<
            GetMobileApplicationsQuery,
            IReadOnlyList<MobileApplicationResponse>
        >(new GetMobileApplicationsQuery());
        _mobileApps = mobRes.IsSuccess ? mobRes.Value : [];

        var sigRes = await QueryDispatcher.QueryAsync<
            GetSignageApplicationsQuery,
            IReadOnlyList<SignageApplicationResponse>
        >(new GetSignageApplicationsQuery());
        _signageApps = sigRes.IsSuccess ? sigRes.Value : [];
    }

    private async Task CreateMobileAppAsync()
    {
        var appRes = await CommandDispatcher.SendAsync<
            CreateMobileApplicationCommand,
            MobileApplicationResponse
        >(new CreateMobileApplicationCommand(_mobileName, _mobileSlug, _bundleId));
        if (appRes.IsSuccess)
        {
            var pageRes = await CommandDispatcher.SendAsync<
                AddMobilePageCommand,
                MobileApplicationResponse
            >(new AddMobilePageCommand(appRes.Value.Id, "Home", "home", "/home", IconName: "Home"));
            if (pageRes.IsSuccess && pageRes.Value.Pages.Count > 0)
            {
                _ = await CommandDispatcher.SendAsync<
                    AddMobileDeepLinkCommand,
                    MobileApplicationResponse
                >(
                    new AddMobileDeepLinkCommand(
                        appRes.Value.Id,
                        "/open/home",
                        "/cms/site",
                        pageRes.Value.Pages[0].Id
                    )
                );
            }
        }

        SetStatus(
            appRes.IsSuccess,
            appRes.IsSuccess
                ? $"Provisioned mobile app '{appRes.Value.Slug}'."
                : appRes.PrimaryError.Description
        );
        await ReloadAsync();
    }

    private async Task CreateSignageAppAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            CreateSignageApplicationCommand,
            SignageApplicationResponse
        >(
            new CreateSignageApplicationCommand(
                _signageName,
                _signageSlug,
                SignagePlatformType.DigitalSignage,
                Slides:
                [
                    new SignageSlideSpec(
                        "Welcome to Our Community Hub",
                        "Check in at the front desk or scan your digital wallet pass",
                        DurationSeconds: 15,
                        Order: 0
                    ),
                ]
            )
        );
        SetStatus(
            res.IsSuccess,
            res.IsSuccess
                ? $"Created signage feed '{res.Value.Slug}'."
                : res.PrimaryError.Description
        );
        await ReloadAsync();
    }

    private void SetStatus(bool ok, string message)
    {
        _isError = !ok;
        _statusMessage = message;
    }
}
