using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Ruth.Host.Middleware;

namespace Ruth.Host.Components.Layout;

/// <summary>Primary Fluent UI Blazor v5 application shell layout.</summary>
public sealed partial class MainLayout : LayoutComponentBase
{
    private static readonly string _themeToggleStyle =
        $"color: {StylesVariables.Colors.Neutral.ForegroundOnBrand};";

    private ErrorBoundary? _errorBoundary;
    private SpotlightSearchModal? _spotlightModal;
    private bool _isAuthenticated;
    private string? _authenticatedUserName;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    internal string BrandTitle =>
        Services.GetService<TenantWorkspaceBrandingState>()?.BrandTitle ?? "Organization Workspace";

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        _errorBoundary?.Recover();

        if (AuthenticationStateTask is null)
        {
            _isAuthenticated = false;
            _authenticatedUserName = null;
            return;
        }

        var state = await AuthenticationStateTask;
        _isAuthenticated = state.User.Identity?.IsAuthenticated == true;
        _authenticatedUserName = state.User.Identity?.Name;
    }

    /// <summary>Toggles between light and dark themes via Fluent UI v5 JS interop.</summary>
    public async Task ToggleThemeAsync()
    {
        _ = await JsRuntime.InvokeAsync<bool>("Blazor.theme.switchTheme");
    }

    /// <summary>Opens the global Spotlight Search modal.</summary>
    public void OpenSpotlight()
    {
        _spotlightModal?.Open();
    }

    /// <summary>Recovers the layout error boundary after a child component failure.</summary>
    public void RecoverFromError()
    {
        _errorBoundary?.Recover();
    }
}
