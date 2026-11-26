using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Contracts.Operators;

/// <summary>Reviews a tenant's go-live submission or lifecycle state from the dedicated Platform Operator console.</summary>
/// <param name="TargetTenantId">The tenant identifier being reviewed.</param>
/// <param name="Decision">The operator review decision.</param>
/// <param name="OperatorEmail">The authenticated platform operator email recording the decision.</param>
/// <param name="Notes">Optional compliance review notes or remediation instructions.</param>
/// <param name="ManualIrsOverrideApproved">Whether the operator manually verified an IRS determination letter when EO BMF returned ManualReviewRequired.</param>
public sealed record ReviewTenantGoLiveCommand(
    Guid TargetTenantId,
    OperatorReviewDecision Decision,
    string OperatorEmail,
    string? Notes = null,
    bool ManualIrsOverrideApproved = false
) : ICommand<TenantComplianceStatusResponse>, IPlatformControlPlaneRequest;
