using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Ruth.Host.Components.Layout;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

public sealed class BlazorShellIntegrationTests
{
    private static readonly string[] _forbiddenV4Markers =
    [
        "FluentDesignTheme",
        "fluent-design-theme",
        "FluentNavMenu",
        "fluent-nav-menu",
        "FluentNavLink",
        "FluentNavGroup",
        "IToastService",
        "IMessageService",
        "Appearance.Accent",
        "An unhandled error has occurred",
    ];

    [Fact]
    public async Task AppShell_SsrHtml_RendersFluentUiV5LayoutLandmarksAndThemeToggle()
    {
        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies();
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<!DOCTYPE html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<html lang=\"en\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<title>Test Organization</title>", html, StringComparison.Ordinal);
        Assert.Contains("<header", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<nav", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<main", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<footer", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<h1>Test Organization</h1>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Ruth RMS", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-layout", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-nav", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Blazor.theme.switchTheme", html, StringComparison.Ordinal);
        Assert.Contains("hidden-if-light", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hidden-if-dark", html, StringComparison.OrdinalIgnoreCase);

        foreach (var marker in _forbiddenV4Markers)
        {
            Assert.DoesNotContain(marker, html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AppShell_UnknownRoute_RendersNotFoundPageCleanlyWithoutServerError()
    {
        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies();
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(
            new Uri("/does-not-exist-shell-route", UriKind.Relative)
        );
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MainLayout_ThemeToggle_InvokesBlazorThemeSwitchThemeJsInterop()
    {
        var jsRuntime = new BlazorTestDoubles.RecordingJsRuntime();
        await using var services = CreateBlazorTestServiceProvider(jsRuntime);
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        MainLayout? capturedLayout = null;

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var host = await renderer.RenderComponentAsync<BlazorTestDoubles.MainLayoutTestHost>(
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        [nameof(BlazorTestDoubles.MainLayoutTestHost.Body)] = (RenderFragment)(
                            static builder => builder.AddContent(0, "Shell Body Content")
                        ),
                        [nameof(BlazorTestDoubles.MainLayoutTestHost.OnLayoutCaptured)] =
                            (Action<MainLayout>)(layout => capturedLayout = layout),
                    }
                )
            );

            await capturedLayout!.ToggleThemeAsync();
            return host.ToHtmlString();
        });

        Assert.Contains("Shell Body Content", html, StringComparison.Ordinal);
        Assert.Contains(
            "Blazor.theme.switchTheme",
            jsRuntime.InvokedIdentifiers,
            StringComparer.Ordinal
        );
    }

    [Fact]
    public async Task MainLayout_ErrorBoundary_CatchesExceptionWithoutLeakingDetailsAndRecovers()
    {
        var jsRuntime = new BlazorTestDoubles.RecordingJsRuntime();
        await using var services = CreateBlazorTestServiceProvider(jsRuntime);
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        var control = new BlazorTestDoubles.ErrorControl { ShouldThrow = false };

        var (host, capturedLayout, capturedChild) = await RenderMainLayoutWithThrowingChildAsync(
            renderer,
            control
        );

        var errorHtml = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            control.ShouldThrow = true;
            capturedChild.TriggerRender();
            await host.QuiescenceTask;
            await Task.Yield();
            return host.ToHtmlString();
        });

        Assert.Contains("Something went wrong", errorHtml, StringComparison.Ordinal);
        Assert.Contains("Try again", errorHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-render-boom", errorHtml, StringComparison.Ordinal);

        control.ShouldThrow = false;
        var recoveredHtml = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            capturedLayout.RecoverFromError();
            await host.QuiescenceTask;
            await Task.Yield();
            return host.ToHtmlString();
        });

        Assert.Contains("Recovered normal content", recoveredHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Something went wrong", recoveredHtml, StringComparison.Ordinal);
    }

    private static async Task<(
        HtmlRootComponent Host,
        MainLayout Layout,
        BlazorTestDoubles.ThrowingChildComponent Child
    )> RenderMainLayoutWithThrowingChildAsync(
        HtmlRenderer renderer,
        BlazorTestDoubles.ErrorControl control
    )
    {
        MainLayout? capturedLayout = null;
        BlazorTestDoubles.ThrowingChildComponent? capturedChild = null;

        var host = await renderer.Dispatcher.InvokeAsync(async () =>
            await renderer.RenderComponentAsync<BlazorTestDoubles.MainLayoutTestHost>(
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        [nameof(BlazorTestDoubles.MainLayoutTestHost.Body)] = (RenderFragment)(
                            builder =>
                            {
                                builder.OpenComponent<BlazorTestDoubles.ThrowingChildComponent>(0);
                                builder.AddAttribute(
                                    1,
                                    nameof(BlazorTestDoubles.ThrowingChildComponent.Control),
                                    control
                                );
                                builder.AddComponentReferenceCapture(2, component =>
                                    capturedChild =
                                        (BlazorTestDoubles.ThrowingChildComponent)component
                                );
                                builder.CloseComponent();
                            }
                        ),
                        [nameof(BlazorTestDoubles.MainLayoutTestHost.OnLayoutCaptured)] =
                            (Action<MainLayout>)(layout => capturedLayout = layout),
                    }
                )
            )
        );

        return (host, capturedLayout!, capturedChild!);
    }

    private static ServiceProvider CreateBlazorTestServiceProvider(
        BlazorTestDoubles.RecordingJsRuntime jsRuntime
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddFluentUIComponents();
        _ = services.AddSingleton<IJSRuntime>(jsRuntime);
        _ = services.AddSingleton<NavigationManager>(new BlazorTestDoubles.TestNavigationManager());
        _ = services.AddSingleton<IErrorBoundaryLogger>(
            new BlazorTestDoubles.TestErrorBoundaryLogger()
        );
        return services.BuildServiceProvider();
    }
}
