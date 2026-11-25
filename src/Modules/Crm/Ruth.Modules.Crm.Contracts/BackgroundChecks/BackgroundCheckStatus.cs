namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Lifecycle status of a person background check.</summary>
public enum BackgroundCheckStatus
{
    /// <summary>Request created locally.</summary>
    Requested = 0,

    /// <summary>Provider invitation sent to candidate.</summary>
    InvitationSent = 1,

    /// <summary>Candidate completed provider invitation form.</summary>
    InvitationCompleted = 2,

    /// <summary>Invitation expired without candidate action.</summary>
    InvitationExpired = 3,

    /// <summary>Screening report is in progress.</summary>
    ReportPending = 4,

    /// <summary>Screening report completed cleanly (clear).</summary>
    ReportClear = 5,

    /// <summary>Screening report completed with findings requiring review (consider).</summary>
    ReportConsider = 6,

    /// <summary>Screening report suspended or disputed.</summary>
    ReportSuspended = 7,
}
