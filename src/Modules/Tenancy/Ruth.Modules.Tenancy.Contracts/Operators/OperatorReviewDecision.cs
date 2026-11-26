namespace Ruth.Modules.Tenancy.Contracts.Operators;

/// <summary>Decision applied by a Platform Operator when reviewing a tenant's go-live compliance submission.</summary>
public enum OperatorReviewDecision
{
    /// <summary>Approves the organization for Live status (requires verified IRS exempt status and Stripe Connect readiness).</summary>
    ApproveLive = 1,

    /// <summary>Returns the organization to Sandbox with remediation notes.</summary>
    ReturnToSandbox = 2,

    /// <summary>Suspends the tenant workspace immediately.</summary>
    Suspend = 3,
}
