using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Ruth.Host.Components;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Host.Middleware;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Host.IntegrationTests;

internal static class W7S7CircuitAndSignalRTestHelper
{
    public static async Task VerifyCircuitImmutabilityAndPrincipalValidationAsync(
        TenantId tenantA,
        TenantId tenantB
    )
    {
        var circuitState = new TenantCircuitState();
        var branding = new TenantWorkspaceBrandingState();
        branding.Initialize(
            new ResolvedHostResponse(
                ResolvedHostKind.TenantWorkspace,
                tenantA,
                "alpha",
                "Alpha Relief",
                TenantLifecycleStage.Live,
                "alpha.sandbox.localhost"
            )
        );
        var authProvider = new MutableTestAuthStateProvider(
            BuildPrincipal(tenantA.Value.ToString("D"), isOperator: false)
        );

        TenantCircuitHandler handler;
        using (AmbientTenantContext.BeginScope(tenantA, "alpha", TenantLifecycleStage.Live))
        {
            handler = new TenantCircuitHandler(circuitState, branding, authProvider);
            await handler.OnCircuitOpenedAsync(null!, CancellationToken.None);
        }

        _ = Assert.Throws<CrossTenantAccessException>(() =>
            circuitState.BindOrVerify(
                ResolvedHostKind.TenantWorkspace,
                new TenantExecutionState(
                    tenantB,
                    "beta",
                    TenantLifecycleStage.Live,
                    IsPlatformOperatorHost: false
                )
            )
        );

        var validator = new HttpContextPrincipalTenantValidator(new HttpContextAccessor());
        var inbound = handler.CreateInboundActivityHandler(_ =>
        {
            Assert.Equal(tenantA, AmbientTenantContext.CurrentTenantId);
            validator.EnsurePrincipalMatchesTenant(AmbientTenantContext.Instance);
            return Task.CompletedTask;
        });
        await inbound(null!);

        using (AmbientTenantContext.BeginScope(tenantB, "beta", TenantLifecycleStage.Live))
        {
            _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() =>
                handler.OnConnectionUpAsync(null!, CancellationToken.None)
            );
        }

        authProvider.SetPrincipal(BuildPrincipal(tid: null, isOperator: false));
        _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() => inbound(null!));

        authProvider.SetPrincipal(BuildPrincipal(tenantB.Value.ToString("D"), isOperator: false));
        _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() => inbound(null!));

        authProvider.SetPrincipal(BuildPrincipal(tid: null, isOperator: true));
        _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() => inbound(null!));
    }

    public static async Task VerifyCrossPlaneBlazorRouterIsolationAsync(TenantId tenantA)
    {
        var opHtml = await RenderRouteForPlaneAsync(
            new ResolvedHostResponse(
                ResolvedHostKind.PlatformOperator,
                TenantId.Empty,
                TenantSlug: null,
                OrganizationName: null,
                TenantLifecycleStage.Unresolved,
                "ops.localhost"
            ),
            "http://ops.localhost/lms/catalog"
        );
        Assert.Contains("Page not found", opHtml, StringComparison.OrdinalIgnoreCase);

        var tenantOpsHtml = await RenderRouteForPlaneAsync(
            new ResolvedHostResponse(
                ResolvedHostKind.TenantWorkspace,
                tenantA,
                "alpha",
                "Alpha Org",
                TenantLifecycleStage.Live,
                "alpha.sandbox.localhost"
            ),
            "http://alpha.sandbox.localhost/ops"
        );
        Assert.Contains("Page not found", tenantOpsHtml, StringComparison.OrdinalIgnoreCase);
    }

    internal static ClaimsPrincipal BuildPrincipal(string? tid, bool isOperator)
    {
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString("D"))];
        if (tid is not null)
        {
            claims.Add(new Claim("tid", tid));
        }

        if (isOperator)
        {
            claims.Add(new Claim("platform_operator", "true"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static async Task<string> RenderRouteForPlaneAsync(
        ResolvedHostResponse resolvedHost,
        string initialUri
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddFluentUIComponents();
        _ = services.AddSingleton<IJSRuntime>(new BlazorTestDoubles.RecordingJsRuntime());
        _ = services.AddSingleton<INavigationInterception>(new NoOpNavigationInterception());
        _ = services.AddSingleton<IScrollToLocationHash>(new NoOpScrollToLocationHash());
        _ = services.AddSingleton<IErrorBoundaryLogger>(
            new BlazorTestDoubles.TestErrorBoundaryLogger()
        );
        var nav = new ConfigurableTestNavigationManager(initialUri);
        _ = services.AddSingleton<NavigationManager>(nav);
        var branding = new TenantWorkspaceBrandingState();
        branding.Initialize(resolvedHost);
        _ = services.AddSingleton(branding);

        await using var sp = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(sp, NullLoggerFactory.Instance);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<Routes>();
            return root.ToHtmlString();
        });
    }

    private sealed class MutableTestAuthStateProvider(ClaimsPrincipal initial)
        : AuthenticationStateProvider
    {
        private ClaimsPrincipal _current = initial;

        public void SetPrincipal(ClaimsPrincipal next)
        {
            _current = next;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_current));
        }
    }

    private sealed class ConfigurableTestNavigationManager : NavigationManager
    {
        public ConfigurableTestNavigationManager(string uri)
        {
            var root = new Uri(uri).GetLeftPart(UriPartial.Authority) + "/";
            Initialize(root, uri);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }

    private sealed class NoOpNavigationInterception : INavigationInterception
    {
        public Task EnableNavigationInterceptionAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpScrollToLocationHash : IScrollToLocationHash
    {
        public Task RefreshScrollPositionForHash(string locationAbsolute)
        {
            return Task.CompletedTask;
        }
    }
}
