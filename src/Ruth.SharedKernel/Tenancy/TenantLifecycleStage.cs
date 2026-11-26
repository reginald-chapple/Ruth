namespace Ruth.SharedKernel.Tenancy;

/// <summary>Operational compliance lifecycle stage of a SaaS organization tenant.</summary>
public enum TenantLifecycleStage
{
    /// <summary>No tenant resolved on the current execution context.</summary>
    Unresolved = 0,

    /// <summary>Newly signed-up nonprofit workspace restricted to setup/sandbox mode.</summary>
    Sandbox = 1,

    /// <summary>Submitted for IRS tax-exempt status and payout verification prior to going live.</summary>
    PendingVerification = 2,

    /// <summary>IRS tax-exempt status verified and approved for live public traffic, payouts, and bulk comms.</summary>
    Live = 3,

    /// <summary>Suspended by a platform operator; public traffic and mutations blocked.</summary>
    Suspended = 4,
}
