namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Classifies the operational purpose of an assistance, scholarship, fee-waiver, or micro-grant program.</summary>
public enum AssistanceProgramCategory
{
    /// <summary>Community or member assistance aid.</summary>
    CommunityAid = 0,

    /// <summary>Tuition, camp, or program scholarship award.</summary>
    Scholarship = 1,

    /// <summary>Dues or invoice fee waiver credit.</summary>
    FeeWaiver = 2,

    /// <summary>Micro-grant or project grant disbursement.</summary>
    MicroGrant = 3,

    /// <summary>Emergency household or individual relief fund.</summary>
    EmergencyRelief = 4,
}
