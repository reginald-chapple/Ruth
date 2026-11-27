using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.SharedKernel.Tests.Tenancy;

public sealed class TenancyPrimitivesAndGuardsTests
{
    private readonly record struct DummyEntityId(Guid Value) : IStronglyTypedId<DummyEntityId>
    {
        public static DummyEntityId Empty => new(Guid.Empty);

        public static DummyEntityId Create()
        {
            return new(Guid.CreateVersion7());
        }

        public static DummyEntityId From(Guid value)
        {
            return new(value);
        }
    }

    private sealed class DummyEntity(DummyEntityId id) : Entity<DummyEntityId>(id);

    private sealed record TenantCmd(string Name) : ICommand;

    private sealed record TenantResultCmd(string Name) : ICommand<string>;

    private sealed record TenantQry(string Filter) : IQuery<string>;

    private sealed record ControlPlaneCmd(string Action)
        : ICommand<string>,
            IPlatformControlPlaneRequest;

    private sealed record ControlPlaneQry(string Filter)
        : IQuery<string>,
            IPlatformControlPlaneRequest;

    private sealed class RecordingPrincipalValidator : IPrincipalTenantValidator
    {
        public int CallCount { get; private set; }

        public bool ThrowMismatch { get; set; }

        public void EnsurePrincipalMatchesTenant(ITenantContext tenantContext)
        {
            CallCount++;
            if (ThrowMismatch)
            {
                throw new CrossTenantAccessException("Principal mismatch.");
            }
        }
    }

    private sealed class PassThroughCmdHandler : ICommandHandler<TenantCmd>
    {
        public Task<Result> HandleAsync(
            TenantCmd command,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class PassThroughResultCmdHandler : ICommandHandler<TenantResultCmd, string>
    {
        public Task<Result<string>> HandleAsync(
            TenantResultCmd command,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Result.Success(command.Name));
        }
    }

    private sealed class PassThroughQryHandler : IQueryHandler<TenantQry, string>
    {
        public Task<Result<string>> HandleAsync(
            TenantQry query,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Result.Success(query.Filter));
        }
    }

    private sealed class PassThroughControlCmdHandler : ICommandHandler<ControlPlaneCmd, string>
    {
        public Task<Result<string>> HandleAsync(
            ControlPlaneCmd command,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Result.Success(command.Action));
        }
    }

    private sealed class PassThroughControlQryHandler : IQueryHandler<ControlPlaneQry, string>
    {
        public Task<Result<string>> HandleAsync(
            ControlPlaneQry query,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Result.Success(query.Filter));
        }
    }

    [Fact]
    public void TenantId_DeriveDeterministicGuid_Produces_Stable_Distinct_Rfc9562V8_Ids_Per_Tenant()
    {
        var seed = new Guid(
            0x11111111,
            0x2222,
            0x3333,
            0x44,
            0x44,
            0x55,
            0x55,
            0x55,
            0x55,
            0x55,
            0x55
        );
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var derivedA1 = tenantA.DeriveDeterministicGuid(seed);
        var derivedA2 = tenantA.DeriveDeterministicGuid(seed);
        var derivedB = tenantB.DeriveDeterministicGuid(seed);

        Assert.Equal(derivedA1, derivedA2);
        Assert.NotEqual(seed, derivedA1);
        Assert.NotEqual(derivedA1, derivedB);
        Assert.Equal(8, derivedA1.Version);
        Assert.Equal(seed, TenantId.Empty.DeriveDeterministicGuid(seed));
    }

    [Fact]
    public void Entity_AssignTenant_Allows_Idempotent_Same_Tenant_And_Blocks_Empty_Or_CrossTenant_Reassignment()
    {
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();
        var entity = new DummyEntity(DummyEntityId.Create());

        _ = Assert.Throws<ArgumentException>(() => entity.AssignTenant(TenantId.Empty));

        entity.AssignTenant(tenantA);
        Assert.Equal(tenantA, entity.TenantId);

        // Idempotent re-assignment to same tenant succeeds
        entity.AssignTenant(tenantA);
        Assert.Equal(tenantA, entity.TenantId);

        // Re-assigning to a different tenant throws CrossTenantAccessException
        _ = Assert.Throws<CrossTenantAccessException>(() => entity.AssignTenant(tenantB));
    }

    [Fact]
    public async Task AmbientTenantContext_Scopes_Nest_Lexically_Across_Async_Flows_And_Fail_Closed()
    {
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            Assert.False(AmbientTenantContext.Instance.HasTenant);
            Assert.False(AmbientTenantContext.Instance.IsLive);
            Assert.Equal(
                TenantLifecycleStage.Unresolved,
                AmbientTenantContext.Instance.LifecycleStage
            );

            using (
                AmbientTenantContext.BeginScope(tenantA, "charity-a", TenantLifecycleStage.Sandbox)
            )
            {
                await Task.Yield();
                Assert.Equal(tenantA, AmbientTenantContext.CurrentTenantId);
                Assert.Equal("charity-a", AmbientTenantContext.Instance.TenantSlug);
                Assert.False(AmbientTenantContext.Instance.IsLive);

                using (
                    AmbientTenantContext.BeginScope(tenantB, "charity-b", TenantLifecycleStage.Live)
                )
                {
                    await Task.Yield();
                    Assert.Equal(tenantB, AmbientTenantContext.CurrentTenantId);
                    Assert.True(AmbientTenantContext.Instance.IsLive);
                }

                Assert.Equal(tenantA, AmbientTenantContext.CurrentTenantId);
                Assert.Equal(
                    TenantLifecycleStage.Sandbox,
                    AmbientTenantContext.Instance.LifecycleStage
                );
            }

            using (AmbientTenantContext.BeginPlatformOperatorScope())
            {
                Assert.True(AmbientTenantContext.Instance.IsPlatformOperatorHost);
                Assert.False(AmbientTenantContext.Instance.HasTenant);
            }

            Assert.False(AmbientTenantContext.Instance.IsPlatformOperatorHost);
            Assert.False(AmbientTenantContext.Instance.HasTenant);
        }
    }

    [Fact]
    public async Task TenantGuard_Blocks_Unresolved_And_Suspended_Tenants_And_Invokes_PrincipalValidator_On_All_Planes()
    {
        var validator = new RecordingPrincipalValidator();
        var voidGuard = new TenantGuardCommandHandler<TenantCmd>(
            new PassThroughCmdHandler(),
            AmbientTenantContext.Instance,
            validator
        );
        var resultGuard = new TenantGuardCommandHandler<TenantResultCmd, string>(
            new PassThroughResultCmdHandler(),
            AmbientTenantContext.Instance,
            validator
        );
        var queryGuard = new TenantGuardQueryHandler<TenantQry, string>(
            new PassThroughQryHandler(),
            AmbientTenantContext.Instance,
            validator
        );
        var controlCmdGuard = new TenantGuardCommandHandler<ControlPlaneCmd, string>(
            new PassThroughControlCmdHandler(),
            AmbientTenantContext.Instance,
            validator
        );
        var controlQryGuard = new TenantGuardQueryHandler<ControlPlaneQry, string>(
            new PassThroughControlQryHandler(),
            AmbientTenantContext.Instance,
            validator
        );

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            _ = await Assert.ThrowsAsync<TenantContextRequiredException>(() =>
                voidGuard.HandleAsync(new TenantCmd("x"))
            );
            _ = await Assert.ThrowsAsync<TenantContextRequiredException>(() =>
                resultGuard.HandleAsync(new TenantResultCmd("x"))
            );
            _ = await Assert.ThrowsAsync<TenantContextRequiredException>(() =>
                queryGuard.HandleAsync(new TenantQry("x"))
            );

            // Control-plane commands/queries succeed without a tenant, while still running principalValidator
            var ctrlCmdRes = await controlCmdGuard.HandleAsync(new ControlPlaneCmd("signup"));
            var ctrlQryRes = await controlQryGuard.HandleAsync(new ControlPlaneQry("queue"));
            Assert.True(ctrlCmdRes.IsSuccess);
            Assert.True(ctrlQryRes.IsSuccess);
            Assert.Equal(2, validator.CallCount);
        }

        var suspendedTenant = TenantId.Create();
        using (
            AmbientTenantContext.BeginScope(
                suspendedTenant,
                "suspended-org",
                TenantLifecycleStage.Suspended
            )
        )
        {
            _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() =>
                voidGuard.HandleAsync(new TenantCmd("x"))
            );
            _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() =>
                resultGuard.HandleAsync(new TenantResultCmd("x"))
            );
            _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() =>
                queryGuard.HandleAsync(new TenantQry("x"))
            );
        }

        var activeTenant = TenantId.Create();
        using (
            AmbientTenantContext.BeginScope(
                activeTenant,
                "active-org",
                TenantLifecycleStage.Sandbox
            )
        )
        {
            Assert.True((await voidGuard.HandleAsync(new TenantCmd("ok"))).IsSuccess);
            Assert.Equal("ok", (await resultGuard.HandleAsync(new TenantResultCmd("ok"))).Value);
            Assert.Equal("q", (await queryGuard.HandleAsync(new TenantQry("q"))).Value);
            Assert.Equal(5, validator.CallCount);

            validator.ThrowMismatch = true;
            _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() =>
                queryGuard.HandleAsync(new TenantQry("q"))
            );
        }
    }
}
