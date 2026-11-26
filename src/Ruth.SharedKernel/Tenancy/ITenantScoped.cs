namespace Ruth.SharedKernel.Tenancy;

/// <summary>Contract implemented by entities owned by an isolated SaaS organization tenant.</summary>
public interface ITenantScoped
{
    /// <summary>Gets the owning organization tenant identifier.</summary>
    TenantId TenantId { get; }

    /// <summary>Assigns the owning tenant identifier once; rejects empty or cross-tenant reassignment.</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    void AssignTenant(TenantId tenantId);
}
