namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Domain context linking an embedded VirtualRoom to another Ruth capability.</summary>
public enum VirtualRoomContextKind
{
    /// <summary>Standalone ad-hoc or recurring virtual meeting room.</summary>
    Standalone = 0,

    /// <summary>Linked to a Groups module Group/Committee/Cohort meeting.</summary>
    GroupMeeting = 1,

    /// <summary>Linked to a Commerce 1-on-1 appointment or paid virtual workshop.</summary>
    CommerceBooking = 2,

    /// <summary>Linked to an Engagement SupportRequest tele-care consultation.</summary>
    SupportTeleCare = 3,

    /// <summary>Linked to an Events module hybrid town-hall, conference, or classroom session.</summary>
    EventHybridSession = 4,

    /// <summary>Linked to an LMS cohort or online class.</summary>
    LmsCohort = 5,
}
