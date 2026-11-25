namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Attendance enrollment rule evaluated when checking in or taking attendance.</summary>
public enum AttendanceRule
{
    /// <summary>Person does not have to belong to the group and is not automatically added.</summary>
    None = 0,

    /// <summary>Automatically add the person with the default role on check-in.</summary>
    AddOnCheckIn = 1,

    /// <summary>Person must already be an active member of the group.</summary>
    AlreadyEnrolledInGroup = 2,
}
