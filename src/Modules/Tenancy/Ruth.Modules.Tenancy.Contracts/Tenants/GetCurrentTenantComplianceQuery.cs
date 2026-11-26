using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Queries the IRS verification, Stripe Connect, custom domain, and go-live compliance status for the active tenant.</summary>
public sealed record GetCurrentTenantComplianceQuery : IQuery<TenantComplianceStatusResponse>;
