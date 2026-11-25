namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>How a group conducts its gatherings.</summary>
public enum MeetingStyle
{
    /// <summary>Meets physically in person.</summary>
    InPerson = 0,

    /// <summary>Meets virtually online.</summary>
    Online = 1,

    /// <summary>Meets both in person and online.</summary>
    Hybrid = 2,
}
