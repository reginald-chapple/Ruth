using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema Branch operating-unit dimension table (reporting.dim_branch).</summary>
public sealed class DimBranch : ITenantScoped
{
    public TenantId TenantId { get; private set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? ShortCode { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset RefreshedAtUtc { get; set; }

    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign {nameof(DimBranch)} from tenant {TenantId} to {tenantId}."
            );
        }

        TenantId = tenantId;
    }
}
