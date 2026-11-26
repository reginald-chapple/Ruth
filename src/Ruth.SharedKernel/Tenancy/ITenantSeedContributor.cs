namespace Ruth.SharedKernel.Tenancy;

/// <summary>Seeds module-owned default records into a newly provisioned organization tenant.</summary>
public interface ITenantSeedContributor
{
    /// <summary>Gets the execution order (lower runs first; Identity/Core = 10..20, downstream = 50+).</summary>
    int Order => 100;

    /// <summary>Idempotently seeds baseline module records inside an active scope for <paramref name="tenantId" />.</summary>
    Task SeedTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}
