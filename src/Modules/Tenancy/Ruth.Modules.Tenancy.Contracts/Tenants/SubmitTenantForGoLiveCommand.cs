using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>
/// Runs automated IRS EO BMF tax-exempt status verification, checks Stripe Connect readiness,
/// and submits the active tenant for Platform Operator go-live approval.
/// </summary>
public sealed record SubmitTenantForGoLiveCommand : ICommand<TenantComplianceStatusResponse>;
