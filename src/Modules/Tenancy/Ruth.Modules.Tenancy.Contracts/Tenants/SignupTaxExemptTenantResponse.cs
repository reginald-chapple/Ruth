using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Response returned when a new tax-exempt organization Sandbox workspace is provisioned.</summary>
/// <param name="TenantId">The newly provisioned tenant identifier.</param>
/// <param name="Slug">The assigned workspace slug.</param>
/// <param name="SandboxHostname">The isolated sandbox hostname assigned to the tenant.</param>
/// <param name="AdminUserId">The initial administrator user account identifier inside the tenant schema.</param>
/// <param name="LifecycleStage">The initial tenant lifecycle stage (<see cref="TenantLifecycleStage.Sandbox" />).</param>
/// <param name="CreatedOnUtc">The UTC creation timestamp.</param>
public sealed record SignupTaxExemptTenantResponse(
    Guid TenantId,
    string Slug,
    string SandboxHostname,
    Guid AdminUserId,
    TenantLifecycleStage LifecycleStage,
    DateTimeOffset CreatedOnUtc
);
