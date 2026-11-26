namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Lifecycle state of a recurring contribution or membership subscription schedule.</summary>
public enum RecurringScheduleStatus
{
    /// <summary>Schedule is active and eligible for settlement.</summary>
    Active = 0,

    /// <summary>Schedule is temporarily paused by constituent or staff.</summary>
    Paused = 1,

    /// <summary>Schedule exceeded consecutive payment failure thresholds and requires billing attention.</summary>
    PastDue = 2,

    /// <summary>Schedule was permanently cancelled.</summary>
    Cancelled = 3,

    /// <summary>Schedule completed its final installment on or after EndDate.</summary>
    Completed = 4,
}
