namespace Ruth.SharedKernel.Tenancy;

/// <summary>Provides active tenant descriptors for fair-share Outbox polling and tenant-iterated background jobs.</summary>
public interface IActiveTenantRegistry
{
    /// <summary>Returns the identifiers of non-suspended tenants eligible for background processing.</summary>
    ValueTask<IReadOnlyList<TenantId>> GetActiveTenantIdsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns full execution states (including slug and compliance lifecycle stage) for non-suspended tenants.</summary>
    async ValueTask<IReadOnlyList<TenantExecutionState>> GetActiveTenantsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var ids = await GetActiveTenantIdsAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return [];
        }

        var states = new TenantExecutionState[ids.Count];
        for (var i = 0; i < ids.Count; i++)
        {
            states[i] = new TenantExecutionState(
                ids[i],
                TenantSlug: null,
                TenantLifecycleStage.Live,
                IsPlatformOperatorHost: false
            );
        }

        return states;
    }
}
