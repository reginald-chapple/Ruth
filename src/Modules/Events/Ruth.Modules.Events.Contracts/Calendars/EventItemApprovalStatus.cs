namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Editorial approval status of an EventItem.</summary>
public enum EventItemApprovalStatus
{
    /// <summary>Draft event item not yet published.</summary>
    Draft = 0,

    /// <summary>Submitted for calendar editor review.</summary>
    PendingApproval = 1,

    /// <summary>Approved for publication on assigned calendars and iCal feeds.</summary>
    Approved = 2,

    /// <summary>Declined by calendar editor.</summary>
    Rejected = 3,
}
