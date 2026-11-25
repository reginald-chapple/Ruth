namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CheckInProfileKind in the CheckIn module.</summary>
public enum CheckInProfileKind
{
    /// <summary>Youth, childcare, or dependent program check-in with guardian pickup codes.</summary>
    YouthAndChildcare = 0,

    /// <summary>Member facility, gym, lounge, makerspace, or co-working entry check-in.</summary>
    MemberFacilityAccess = 1,

    /// <summary>Class, cohort, workshop, or structured program attendance check-in.</summary>
    ClassesAndPrograms = 2,

    /// <summary>Volunteer, staff, or committee shift roster check-in.</summary>
    VolunteerAndStaffShifts = 3,

    /// <summary>Conference, gala, or general event session check-in.</summary>
    EventAndConference = 4,
}
