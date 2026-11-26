namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Computed outreach cadence health status for a ConstituentContact.</summary>
public enum ConstituentHealthStatus
{
    /// <summary>Within target outreach cadence window.</summary>
    Healthy = 0,

    /// <summary>Next touchpoint is due today or within 2 days.</summary>
    DueSoon = 1,

    /// <summary>Past target cadence date.</summary>
    Overdue = 2,

    /// <summary>Overdue by more than an entire cadence cycle.</summary>
    CriticalLapse = 3,

    /// <summary>Portfolio contact is inactive/paused.</summary>
    Paused = 4,
}
