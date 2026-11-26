namespace Ruth.SharedKernel.Tenancy;

/// <summary>Provides active tenant identifiers for fair-share Outbox polling and tenant-iterated background jobs.</summary>
public interface IActiveTenantRegistry
{
    /// <summary>Returns the identifiers of non-suspended tenants eligible for background processing.</summary>
    ValueTask<IReadOnlyList<TenantId>> GetActiveTenantIdsAsync(
        CancellationToken cancellationToken = default
    );
}
