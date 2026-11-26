namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Lifecycle status of a live SignalR interactive session.</summary>
public enum InteractiveSessionStatus
{
    /// <summary>Configured session not yet live.</summary>
    Draft = 0,

    /// <summary>Live session accepting audience responses and broadcasting stage updates.</summary>
    Live = 1,

    /// <summary>Concluded session.</summary>
    Ended = 2,
}
