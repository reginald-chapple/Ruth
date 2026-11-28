using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Ruth.Host.Middleware;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Host.IntegrationTests;

public sealed class W7S7SecurityPrimitivesUnitTests
{
    [Fact]
    public void TenantCircuitState_HandshakePlanes_AndPrincipalGuards_EnforceEveryBranch()
    {
        var tenantId = TenantId.Create();
        var unbound = new TenantCircuitState();

        _ = Assert.Throws<TenantContextRequiredException>(() =>
            unbound.EnsureCompatibleAmbientState(TenantExecutionState.Unresolved)
        );
        _ = Assert.Throws<TenantContextRequiredException>(() =>
            unbound.ValidateAndCapturePrincipal(principal: null)
        );
        _ = Assert.Throws<TenantContextRequiredException>(() =>
            unbound.BindOrVerify(ResolvedHostKind.Unknown, TenantExecutionState.Unresolved)
        );
        _ = Assert.Throws<TenantContextRequiredException>(() =>
            unbound.BindOrVerify(ResolvedHostKind.TenantWorkspace, TenantExecutionState.Unresolved)
        );
        _ = Assert.Throws<CrossTenantAccessException>(() =>
            unbound.BindOrVerify(ResolvedHostKind.PlatformOperator, TenantExecutionState.Unresolved)
        );
        _ = Assert.Throws<CrossTenantAccessException>(() =>
            unbound.BindOrVerify(
                ResolvedHostKind.PlatformSignup,
                new TenantExecutionState(
                    tenantId,
                    "alpha",
                    TenantLifecycleStage.Sandbox,
                    IsPlatformOperatorHost: false
                )
            )
        );

        VerifyOperatorCircuitPrincipalGuards();
        VerifySignupCircuitPrincipalGuards(tenantId);
    }

    [Fact]
    public void HttpContextPrincipalTenantValidator_EnforcesOperatorAndTenantBoundaries()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var validator = new HttpContextPrincipalTenantValidator(accessor);
        var tenantId = TenantId.Create();

        // Anonymous principal passes
        using (AmbientTenantContext.BeginScope(tenantId, "alpha", TenantLifecycleStage.Live))
        {
            validator.EnsurePrincipalMatchesTenant(AmbientTenantContext.Instance);
        }

        // Tenant principal on Platform Operator host is rejected
        accessor.HttpContext.User = W7S7CircuitAndSignalRTestHelper.BuildPrincipal(
            tenantId.Value.ToString("D"),
            isOperator: false
        );
        using (AmbientTenantContext.BeginPlatformOperatorScope())
        {
            _ = Assert.Throws<CrossTenantAccessException>(() =>
                validator.EnsurePrincipalMatchesTenant(AmbientTenantContext.Instance)
            );
        }

        // Operator principal on Platform Operator host succeeds
        accessor.HttpContext.User = W7S7CircuitAndSignalRTestHelper.BuildPrincipal(
            tid: null,
            isOperator: true
        );
        using (AmbientTenantContext.BeginPlatformOperatorScope())
        {
            validator.EnsurePrincipalMatchesTenant(AmbientTenantContext.Instance);
        }

        // Operator principal on Unresolved/Signup host is rejected
        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            _ = Assert.Throws<CrossTenantAccessException>(() =>
                validator.EnsurePrincipalMatchesTenant(AmbientTenantContext.Instance)
            );
        }

        // Non-operator principal on Unresolved/Signup host is ignored by tenant tid check
        accessor.HttpContext.User = W7S7CircuitAndSignalRTestHelper.BuildPrincipal(
            tid: null,
            isOperator: false
        );
        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            validator.EnsurePrincipalMatchesTenant(AmbientTenantContext.Instance);
        }
    }

    [Fact]
    public void TenantStorageKeyScope_RejectsTraversal_ControlChars_EmptyKeys_AndIdempotentlyPrefixes()
    {
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            _ = Assert.Throws<TenantContextRequiredException>(() =>
                TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, "docs/a.pdf")
            );
        }

        using (AmbientTenantContext.BeginScope(tenantA, "alpha", TenantLifecycleStage.Live))
        {
            var ctx = AmbientTenantContext.Instance;
            _ = Assert.Throws<ArgumentException>(() =>
                TenantStorageKeyScope.ResolveKey(ctx, "   ")
            );
            _ = Assert.Throws<CrossTenantAccessException>(() =>
                TenantStorageKeyScope.ResolveKey(ctx, "../secrets.txt")
            );
            _ = Assert.Throws<CrossTenantAccessException>(() =>
                TenantStorageKeyScope.ResolveKey(ctx, "docs/\0bad.pdf")
            );
            _ = Assert.Throws<CrossTenantAccessException>(() =>
                TenantStorageKeyScope.ResolveKey(ctx, $"tenants/{tenantA}/   ")
            );
            _ = Assert.Throws<CrossTenantAccessException>(() =>
                TenantStorageKeyScope.ResolveKey(ctx, $"tenants/{tenantB}/docs/a.pdf")
            );

            var canonical = TenantStorageKeyScope.ResolveKey(ctx, "docs\\report.pdf");
            Assert.Equal($"tenants/{tenantA}/docs/report.pdf", canonical);
            Assert.Equal(canonical, TenantStorageKeyScope.ResolveKey(ctx, canonical));
        }
    }

    private static void VerifyOperatorCircuitPrincipalGuards()
    {
        var opState = new TenantExecutionState(
            TenantId.Empty,
            TenantSlug: null,
            TenantLifecycleStage.Unresolved,
            IsPlatformOperatorHost: true
        );
        var opCircuit = new TenantCircuitState();
        opCircuit.BindOrVerify(ResolvedHostKind.PlatformOperator, opState);
        opCircuit.BindOrVerify(ResolvedHostKind.PlatformOperator, opState);
        opCircuit.EnsureCompatibleAmbientState(TenantExecutionState.Unresolved);
        opCircuit.EnsureCompatibleAmbientState(opState);

        opCircuit.ValidateAndCapturePrincipal(
            W7S7CircuitAndSignalRTestHelper.BuildPrincipal(tid: null, isOperator: true)
        );
        _ = Assert.Throws<CrossTenantAccessException>(() =>
            opCircuit.ValidateAndCapturePrincipal(
                W7S7CircuitAndSignalRTestHelper.BuildPrincipal(tid: null, isOperator: false)
            )
        );
    }

    private static void VerifySignupCircuitPrincipalGuards(TenantId tenantId)
    {
        var signupCircuit = new TenantCircuitState();
        signupCircuit.BindOrVerify(
            ResolvedHostKind.PlatformSignup,
            TenantExecutionState.Unresolved
        );
        signupCircuit.ValidateAndCapturePrincipal(new ClaimsPrincipal(new ClaimsIdentity()));
        signupCircuit.ValidateAndCapturePrincipal(
            W7S7CircuitAndSignalRTestHelper.BuildPrincipal(tid: null, isOperator: false)
        );
        _ = Assert.Throws<CrossTenantAccessException>(() =>
            signupCircuit.ValidateAndCapturePrincipal(
                W7S7CircuitAndSignalRTestHelper.BuildPrincipal(
                    tenantId.Value.ToString("D"),
                    isOperator: false
                )
            )
        );
    }
}
