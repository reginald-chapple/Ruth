namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Stewardship or billing follow-up workflow status for a cadence alert.</summary>
public enum CadenceAlertStatus
{
    /// <summary>Newly detected alert awaiting review.</summary>
    Open = 0,

    /// <summary>Acknowledged by staff for follow-up.</summary>
    Acknowledged = 1,

    /// <summary>Resolved after outreach or billing recovery.</summary>
    Resolved = 2,

    /// <summary>Dismissed as expected behavior.</summary>
    Dismissed = 3,
}
