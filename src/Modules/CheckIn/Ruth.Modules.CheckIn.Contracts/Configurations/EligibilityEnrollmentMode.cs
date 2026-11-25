namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type EligibilityEnrollmentMode in the CheckIn module.</summary>
public enum EligibilityEnrollmentMode
{
    /// <summary>Any participant satisfying the demographic, tier, and skill rules may check in.</summary>
    OpenToEligible = 0,

    /// <summary>Participant must already be an active member of the area's linked group.</summary>
    PreEnrolledMembersOnly = 1,

    /// <summary>Participant must hold a confirmed shift assignment for the active schedule.</summary>
    ScheduledShiftOnly = 2,
}
