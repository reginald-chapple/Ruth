using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Ruth.Host.Components.Layout;
using Ruth.Host.Components.Pages;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Xunit;

namespace Ruth.Host.IntegrationTests;

public sealed class OnboardingComponentIntegrationTests
{
    [Fact]
    public async Task OnboardingComponent_RendersFluentUiV5ControlsAndSubmitsCommand()
    {
        var orgId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var dispatcher = new OnboardingTestDoubles.RecordingCommandDispatcher(_ =>
            Result.Success(new CompleteOnboardingResponse(orgId, userId, DateTimeOffset.UtcNow))
        );
        var stateReader = new CompletedOnboardingStateReaderStub(isOnboardingRequired: true);
        var navigation = new BlazorTestDoubles.TestNavigationManager();

        await using var services = CreateComponentServices(dispatcher, stateReader, navigation);
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        Onboarding? capturedPage = null;
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root =
                await renderer.RenderComponentAsync<OnboardingTestDoubles.OnboardingTestHost>(
                    ParameterView.FromDictionary(
                        new Dictionary<string, object?>(StringComparer.Ordinal)
                        {
                            [nameof(OnboardingTestDoubles.OnboardingTestHost.OnPageCaptured)] =
                                (Action<Onboarding>)(page => capturedPage = page),
                        }
                    )
                );
            return root.ToHtmlString();
        });

        Assert.Contains("<h1>Welcome to Ruth RMS", html, StringComparison.Ordinal);
        Assert.Contains("<h2 id=\"org-section-heading\">", html, StringComparison.Ordinal);
        Assert.Contains("<h2 id=\"admin-section-heading\">", html, StringComparison.Ordinal);
        Assert.Contains("fluent-text-input", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-option", html, StringComparison.OrdinalIgnoreCase);
        OnboardingTestDoubles.AssertTimeZoneDropdownOmitsRequired(html);
        Assert.Contains("fluent-button", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fluent-text-field", html, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(capturedPage);
        capturedPage.Model.OrganizationName = "Civic Foundation";
        capturedPage.Model.TimeZoneId = "UTC";
        capturedPage.Model.PrimaryBranchName = "Main Branch";
        capturedPage.Model.PrimaryBranchShortCode = "  MAIN ";
        capturedPage.Model.AdminDisplayName = "Civic Admin";
        capturedPage.Model.AdminUserName = "admin";
        capturedPage.Model.AdminEmail = "admin@civic.org";
        capturedPage.Model.AdminPassword = "StrongPass#2026!";
        capturedPage.Model.ConfirmPassword = "StrongPass#2026!";

        await renderer.Dispatcher.InvokeAsync(capturedPage.SubmitAsync);

        var command = Assert.IsType<CompleteOnboardingCommand>(
            Assert.Single(dispatcher.SentCommands)
        );
        Assert.Equal("Civic Foundation", command.OrganizationName);
        Assert.Equal("MAIN", command.PrimaryBranchShortCode);
        Assert.Equal(1, stateReader.InvalidateCount);
        Assert.Contains("/", navigation.NavigatedUris, StringComparer.Ordinal);
    }

    [Fact]
    public async Task OnboardingComponent_WhenCommandFails_RendersErrorMessageBarWithoutStackTrace()
    {
        var dispatcher = new OnboardingTestDoubles.RecordingCommandDispatcher(_ =>
            Result.Failure<CompleteOnboardingResponse>(
                DomainError.Conflict(
                    "Identity.OnboardingAlreadyCompleted",
                    "Setup is already locked."
                )
            )
        );
        var stateReader = new CompletedOnboardingStateReaderStub(isOnboardingRequired: true);
        var navigation = new BlazorTestDoubles.TestNavigationManager();

        await using var services = CreateComponentServices(dispatcher, stateReader, navigation);
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        Onboarding? capturedPage = null;
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root =
                await renderer.RenderComponentAsync<OnboardingTestDoubles.OnboardingTestHost>(
                    ParameterView.FromDictionary(
                        new Dictionary<string, object?>(StringComparer.Ordinal)
                        {
                            [nameof(OnboardingTestDoubles.OnboardingTestHost.OnPageCaptured)] =
                                (Action<Onboarding>)(page => capturedPage = page),
                        }
                    )
                );
            await capturedPage!.SubmitAsync();
            await root.QuiescenceTask;
            await Task.Yield();
            return root.ToHtmlString();
        });

        Assert.Contains("Unable to complete setup", html, StringComparison.Ordinal);
        Assert.Contains("Setup is already locked.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", html, StringComparison.Ordinal);
        Assert.Empty(navigation.NavigatedUris);
    }

    [Fact]
    public async Task OnboardingComponent_WhenAlreadyCompleted_RedirectsToHomeImmediately()
    {
        var dispatcher = new OnboardingTestDoubles.RecordingCommandDispatcher();
        var stateReader = new CompletedOnboardingStateReaderStub(isOnboardingRequired: false);
        var navigation = new BlazorTestDoubles.TestNavigationManager();

        await using var services = CreateComponentServices(dispatcher, stateReader, navigation);
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        _ = await renderer.Dispatcher.InvokeAsync(renderer.RenderComponentAsync<Onboarding>);

        Assert.Contains("/", navigation.NavigatedUris, StringComparer.Ordinal);
    }

    [Fact]
    public async Task OnboardingLayout_OmitsNavLinksAndSupportsThemeToggleAndErrorRecovery()
    {
        var jsRuntime = new BlazorTestDoubles.RecordingJsRuntime();
        await using var services = CreateLayoutServices(jsRuntime);
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        var control = new BlazorTestDoubles.ErrorControl { ShouldThrow = false };
        OnboardingLayout? capturedLayout = null;
        BlazorTestDoubles.ThrowingChildComponent? capturedChild = null;

        var host = await renderer.Dispatcher.InvokeAsync(async () =>
            await renderer.RenderComponentAsync<OnboardingTestDoubles.OnboardingLayoutTestHost>(
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        [nameof(OnboardingTestDoubles.OnboardingLayoutTestHost.Body)] =
                            (RenderFragment)(
                                builder =>
                                {
                                    builder.OpenComponent<BlazorTestDoubles.ThrowingChildComponent>(
                                        0
                                    );
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
                        [nameof(OnboardingTestDoubles.OnboardingLayoutTestHost.OnLayoutCaptured)] =
                            (Action<OnboardingLayout>)(layout => capturedLayout = layout),
                    }
                )
            )
        );

        var initialHtml = await renderer.Dispatcher.InvokeAsync(() =>
        {
            return host.ToHtmlString();
        });
        Assert.DoesNotContain("<nav", initialHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fluent-nav", initialHtml, StringComparison.OrdinalIgnoreCase);

        await renderer.Dispatcher.InvokeAsync(() => capturedLayout!.ToggleThemeAsync());
        Assert.Contains(
            "Blazor.theme.switchTheme",
            jsRuntime.InvokedIdentifiers,
            StringComparer.Ordinal
        );

        var errorHtml = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            control.ShouldThrow = true;
            capturedChild!.TriggerRender();
            await host.QuiescenceTask;
            await Task.Yield();
            return host.ToHtmlString();
        });
        Assert.Contains("Something went wrong", errorHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-render-boom", errorHtml, StringComparison.Ordinal);

        control.ShouldThrow = false;
        var recoveredHtml = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            capturedLayout!.RecoverFromError();
            await host.QuiescenceTask;
            await Task.Yield();
            return host.ToHtmlString();
        });
        Assert.Contains("Recovered normal content", recoveredHtml, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateComponentServices(
        ICommandDispatcher dispatcher,
        IOnboardingStateReader stateReader,
        NavigationManager navigation
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddFluentUIComponents();
        _ = services.AddSingleton(dispatcher);
        _ = services.AddSingleton(stateReader);
        _ = services.AddSingleton(navigation);
        _ = services.AddSingleton<IJSRuntime>(new BlazorTestDoubles.RecordingJsRuntime());
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateLayoutServices(
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
