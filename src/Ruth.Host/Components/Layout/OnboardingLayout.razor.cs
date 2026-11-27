using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Ruth.Host.Middleware;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Components.Layout;

/// <summary>Minimal navigation-free Fluent UI Blazor v5 layout for setup and control-plane workflows.</summary>
public sealed partial class OnboardingLayout : LayoutComponentBase
{
    private static readonly string _themeToggleStyle =
        $"color: {StylesVariables.Colors.Neutral.ForegroundOnBrand};";

    private ErrorBoundary? _errorBoundary;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    internal string BrandTitle =>
        Services.GetService<TenantWorkspaceBrandingState>()?.BrandTitle ?? "Workspace Setup";

    internal string FooterSubtitle
    {
        get
        {
            var tenantContext = Services.GetService<ITenantContext>();
            return tenantContext?.IsPlatformOperatorHost == true ? "Platform Compliance Operations"
                : tenantContext?.HasTenant == false ? "Nonprofit Workspace Provisioning"
                : "Initial Workspace Setup";
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _errorBoundary?.Recover();
    }

    /// <summary>Toggles between light and dark themes via Fluent UI v5 JS interop.</summary>
    public async Task ToggleThemeAsync()
    {
        _ = await JsRuntime.InvokeAsync<bool>("Blazor.theme.switchTheme");
    }

    /// <summary>Recovers the layout error boundary after a child component failure.</summary>
    public void RecoverFromError()
    {
        _errorBoundary?.Recover();
    }
}
