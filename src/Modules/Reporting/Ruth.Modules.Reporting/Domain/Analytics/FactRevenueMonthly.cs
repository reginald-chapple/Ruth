using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema monthly revenue &amp; tax-deductibility fact table (reporting.fact_revenue_monthly).</summary>
public sealed class FactRevenueMonthly : ITenantScoped
{
    public TenantId TenantId { get; private set; }
    public Guid Id { get; set; }
    public int MonthDateKey { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public decimal TaxDeductibleGiving { get; set; }
    public decimal NonDeductibleDuesAndFees { get; set; }
    public int TransactionCount { get; set; }
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
                $"Cannot reassign {nameof(FactRevenueMonthly)} from tenant {TenantId} to {tenantId}."
            );
        }

        TenantId = tenantId;
    }
}
