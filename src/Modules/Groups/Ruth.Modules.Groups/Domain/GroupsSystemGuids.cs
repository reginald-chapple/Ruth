namespace Ruth.Modules.Groups.Domain;

/// <summary>Stable deterministic SystemGuid constants for Groups bounded-context entities.</summary>
public static class GroupsSystemGuids
{
    /// <summary>EntityType SystemGuid for GroupType.</summary>
    public static readonly Guid GroupType = new(
        0x0dd30b04,
        0x01cf,
        0x4b38,
        0x8e,
        0x83,
        0xbe,
        0x66,
        0x1e,
        0x2f,
        0x72,
        0x86
    );

    /// <summary>EntityType SystemGuid for GroupTypeRole.</summary>
    public static readonly Guid GroupTypeRole = new(
        0xd155c373,
        0x9e47,
        0x4c6a,
        0xba,
        0xdd,
        0x79,
        0x2f,
        0x31,
        0xaf,
        0x5f,
        0xba
    );

    /// <summary>EntityType SystemGuid for Group.</summary>
    public static readonly Guid Group = new(
        0x9bbda8a3,
        0xa27b,
        0x4966,
        0x85,
        0x1c,
        0x1f,
        0xb6,
        0x46,
        0x31,
        0x22,
        0x77
    );

    /// <summary>EntityType SystemGuid for GroupLocation.</summary>
    public static readonly Guid GroupLocation = new(
        0x26248ee7,
        0x09f3,
        0x4578,
        0xa1,
        0xd6,
        0x47,
        0xe0,
        0x1d,
        0x91,
        0xd6,
        0xef
    );

    /// <summary>EntityType SystemGuid for GroupMember.</summary>
    public static readonly Guid GroupMember = new(
        0x49668c74,
        0x9604,
        0x4666,
        0xa3,
        0xc7,
        0x3d,
        0x1d,
        0x90,
        0x1c,
        0x8e,
        0x2e
    );

    /// <summary>EntityType SystemGuid for GroupRequirementType.</summary>
    public static readonly Guid GroupRequirementType = new(
        0x8e67e852,
        0xd1bf,
        0x485c,
        0x98,
        0x98,
        0x09,
        0xf1,
        0x99,
        0x98,
        0xcc,
        0x40
    );

    /// <summary>EntityType SystemGuid for GroupRequirement.</summary>
    public static readonly Guid GroupRequirement = new(
        0xcfc7de86,
        0x222e,
        0x4669,
        0x83,
        0xc2,
        0xa3,
        0xf5,
        0xb0,
        0x4c,
        0xb5,
        0xd6
    );

    /// <summary>EntityType SystemGuid for GroupMemberRequirement.</summary>
    public static readonly Guid GroupMemberRequirement = new(
        0xff1b2c4b,
        0x0f2d,
        0x4d9b,
        0x9e,
        0x85,
        0x73,
        0x36,
        0xcc,
        0xc2,
        0x4a,
        0x62
    );

    /// <summary>EntityType SystemGuid for GroupSyncRule.</summary>
    public static readonly Guid GroupSyncRule = new(
        0x1c011499,
        0x1122,
        0x4429,
        0x9a,
        0xfa,
        0x65,
        0x78,
        0x79,
        0x8e,
        0x18,
        0xa9
    );

    /// <summary>EntityType SystemGuid for AttendanceOccurrence.</summary>
    public static readonly Guid AttendanceOccurrence = new(
        0x0f6fd7f1,
        0x7af5,
        0x4135,
        0x84,
        0x3f,
        0xe3,
        0x49,
        0x48,
        0xd4,
        0xea,
        0x28
    );

    /// <summary>EntityType SystemGuid for Attendance.</summary>
    public static readonly Guid Attendance = new(
        0x091a5d54,
        0x832a,
        0x4c32,
        0xa6,
        0x22,
        0x9f,
        0x46,
        0x9a,
        0x67,
        0x60,
        0x82
    );
}
