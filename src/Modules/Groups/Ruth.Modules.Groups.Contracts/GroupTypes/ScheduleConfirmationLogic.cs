namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>How volunteer schedule assignments transition to confirmed status.</summary>
public enum ScheduleConfirmationLogic
{
    /// <summary>Volunteer must explicitly accept or decline via RSVP.</summary>
    Ask = 0,

    /// <summary>Volunteer is automatically confirmed upon scheduling.</summary>
    AutoAccept = 1,
}
