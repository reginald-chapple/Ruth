using Ruth.Modules.Tenancy.Contracts.Operators;

namespace Ruth.Modules.Tenancy.Slices.Endpoints;

/// <summary>Request payload submitted by an authenticated Platform Operator to review a tenant's Go-Live status.</summary>
/// <param name="TargetTenantId">The tenant identifier being reviewed.</param>
/// <param name="Decision">The operator review decision.</param>
/// <param name="Notes">Optional compliance review notes or remediation instructions.</param>
/// <param name="ManualIrsOverrideApproved">Whether the operator manually verified an IRS determination letter when EO BMF returned ManualReviewRequired.</param>
public sealed record ReviewTenantGoLiveRequest(
    Guid TargetTenantId,
    OperatorReviewDecision Decision,
    string? Notes = null,
    bool ManualIrsOverrideApproved = false
);
