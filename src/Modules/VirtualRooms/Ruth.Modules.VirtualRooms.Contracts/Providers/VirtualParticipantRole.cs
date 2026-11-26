namespace Ruth.Modules.VirtualRooms.Contracts.Providers;

/// <summary>Room media capability role mapped from Ruth ACL permissions.</summary>
public enum VirtualParticipantRole
{
    /// <summary>Room administrator/host able to moderate, publish, subscribe, and trigger egress recording.</summary>
    HostModerator = 0,

    /// <summary>Presenter able to publish audio/video/screen and data tracks.</summary>
    Presenter = 1,

    /// <summary>Interactive attendee able to publish audio/video and subscribe.</summary>
    Participant = 2,

    /// <summary>Audience observer able to subscribe without publishing media tracks.</summary>
    Observer = 3,
}
