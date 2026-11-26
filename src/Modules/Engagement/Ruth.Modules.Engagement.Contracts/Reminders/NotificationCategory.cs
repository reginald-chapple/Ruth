namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Category of an in-app NotificationMessage in the staff/constituent notification bell.</summary>
public enum NotificationCategory
{
    /// <summary>Entity follow-up reminder.</summary>
    Reminder = 0,

    /// <summary>Community care / support alert.</summary>
    SupportCare = 1,

    /// <summary>Pipeline coordinator assignment.</summary>
    PipelineAssignment = 2,

    /// <summary>Consolidated Following subscription digest.</summary>
    FollowingDigest = 3,

    /// <summary>Earned achievement celebration.</summary>
    Achievement = 4,

    /// <summary>Portfolio touchpoint due alert.</summary>
    TouchpointDue = 5,
}
