using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Ruth.Modules.Search.Contracts.Querying;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;

namespace Ruth.Host.Components.Layout;

/// <summary>Global keyboard-driven (Cmd+K / Ctrl+K) Spotlight Search modal in the Staff Shell.</summary>
public sealed partial class SpotlightSearchModal : ComponentBase, IAsyncDisposable
{
    private static readonly string[] _moduleFilters =
    [
        "All",
        "Crm",
        "Groups",
        "Finance",
        "Events",
        "Commerce",
        "Engagement",
        "Workflow",
        "Cms",
        "Lms",
    ];

    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<SpotlightSearchModal>? _dotNetRef;
    private bool _isOpen;
    private string _queryText = string.Empty;
    private string _selectedModule = "All";
    private SearchResultsResponse? _results;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./ruth-spotlight.js"
            );
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsModule.InvokeVoidAsync("registerSpotlightShortcut", _dotNetRef);
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected during render.
        }
        catch (InvalidOperationException)
        {
            // Static SSR prerender does not support JS interop.
        }
    }

    public void Open()
    {
        _isOpen = true;
        StateHasChanged();
    }

    public void Close()
    {
        _isOpen = false;
        StateHasChanged();
    }

    [JSInvokable]
    public void ToggleSpotlightFromShortcut()
    {
        _isOpen = !_isOpen;
        StateHasChanged();
    }

    [JSInvokable]
    public void CloseSpotlightFromShortcut()
    {
        if (_isOpen)
        {
            _isOpen = false;
            StateHasChanged();
        }
    }

    private async Task ToggleModuleAsync(string module)
    {
        _selectedModule = module;
        await ExecuteSearchAsync();
    }

    private async Task ExecuteSearchAsync()
    {
        var subject = await ResolveSubjectAsync();
        IReadOnlyList<string>? modules = string.Equals(
            _selectedModule,
            "All",
            StringComparison.OrdinalIgnoreCase
        )
            ? null
            : [_selectedModule];

        var res = await QueryDispatcher.QueryAsync<
            ExecuteUniversalSearchQuery,
            SearchResultsResponse
        >(
            new ExecuteUniversalSearchQuery(
                _queryText,
                subject,
                ModuleNames: modules,
                PublicOnly: !subject.IsAuthenticated,
                Limit: 20
            )
        );
        if (res.IsSuccess)
        {
            _results = res.Value;
        }
    }

    private async Task<SecuritySubject> ResolveSubjectAsync()
    {
        if (AuthenticationStateTask is null)
        {
            return SecuritySubject.Anonymous;
        }

        var state = await AuthenticationStateTask;
        var user = state.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return SecuritySubject.Anonymous;
        }

        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        Guid? userId = Guid.TryParse(sub, out var parsed) ? parsed : null;
        HashSet<Guid> roles =
        [
            .. user.FindAll("role_id")
                .Select(static c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty)
                .Where(static g => g != Guid.Empty),
        ];
        return new SecuritySubject(userId, roles);
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("unregisterSpotlightShortcut");
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Ignore disconnected circuit cleanup.
            }
        }

        _dotNetRef?.Dispose();
    }
}
