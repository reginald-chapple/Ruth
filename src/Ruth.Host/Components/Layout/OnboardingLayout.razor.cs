using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Ruth.Host.Components.Layout;

/// <summary>Focused Fluent UI Blazor v5 layout used exclusively for first-run onboarding.</summary>
public sealed partial class OnboardingLayout : LayoutComponentBase
{
    private static readonly string _themeToggleStyle =
        $"color: {StylesVariables.Colors.Neutral.ForegroundOnBrand};";

    private ErrorBoundary? _errorBoundary;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

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
