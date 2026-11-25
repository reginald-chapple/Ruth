namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>How a requirement due date is computed for a group member.</summary>
public enum RequirementDueDateType
{
    /// <summary>Due immediately upon joining.</summary>
    Immediate = 0,

    /// <summary>Due on a static configured calendar date.</summary>
    ConfiguredDate = 1,

    /// <summary>Due relative to a date attribute on the group plus offset days.</summary>
    GroupAttribute = 2,

    /// <summary>Due a configured number of days after joining the group.</summary>
    DaysAfterJoining = 3,
}
