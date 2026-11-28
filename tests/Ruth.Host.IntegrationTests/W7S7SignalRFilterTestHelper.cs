using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Host.Middleware;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Host.IntegrationTests;

internal static class W7S7SignalRFilterTestHelper
{
    public static async Task VerifySignalRHubFilterEnforcesHandshakeTenantAsync(
        TenantId tenantA,
        TenantId tenantB
    )
    {
        var filter = new TenantSignalRHubFilter();
        var items = new Dictionary<object, object?>();
        var callerCtx = new FakeHubCallerContext(
            items,
            W7S7CircuitAndSignalRTestHelper.BuildPrincipal(
                tenantA.Value.ToString("D"),
                isOperator: false
            )
        );
        var lifetimeCtx = new HubLifetimeContext(
            callerCtx,
            new ServiceCollection().BuildServiceProvider(),
            new DummyHub()
        );

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            _ = await Assert.ThrowsAsync<TenantContextRequiredException>(() =>
                filter.OnConnectedAsync(lifetimeCtx, static _ => Task.CompletedTask)
            );
        }

        using (AmbientTenantContext.BeginScope(tenantA, "alpha", TenantLifecycleStage.Live))
        {
            await filter.OnConnectedAsync(lifetimeCtx, static _ => Task.CompletedTask);
        }

        var invCtx = new HubInvocationContext(
            callerCtx,
            new ServiceCollection().BuildServiceProvider(),
            new DummyHub(),
            typeof(DummyHub).GetMethod(nameof(DummyHub.Ping))!,
            []
        );

        var observed = await filter.InvokeMethodAsync(invCtx, static _ =>
            ValueTask.FromResult<object?>(AmbientTenantContext.CurrentTenantId)
        );
        Assert.Equal(tenantA, observed);

        using (AmbientTenantContext.BeginScope(tenantB, "beta", TenantLifecycleStage.Live))
        {
            _ = await Assert.ThrowsAsync<CrossTenantAccessException>(async () =>
                _ = await filter.InvokeMethodAsync(invCtx, static _ =>
                    ValueTask.FromResult<object?>(null)
                )
            );
        }
    }

    private sealed class DummyHub : Hub
    {
        public static void Ping() { }
    }

    private sealed class FakeHubCallerContext(
        IDictionary<object, object?> items,
        ClaimsPrincipal user
    ) : HubCallerContext
    {
        public override string ConnectionId => "conn-test";
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User => user;
        public override IDictionary<object, object?> Items => items;
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features =>
            new Microsoft.AspNetCore.Http.Features.FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;

        public override void Abort() { }
    }
}
