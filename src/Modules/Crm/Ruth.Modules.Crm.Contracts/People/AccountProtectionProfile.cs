namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Protection level governing sensitive person records and merge security guardrails.</summary>
public enum AccountProtectionProfile
{
    /// <summary>Standard account protection.</summary>
    Low = 0,

    /// <summary>Elevated protection.</summary>
    Medium = 1,

    /// <summary>High protection requiring explicit elevated permission to merge.</summary>
    High = 2,

    /// <summary>Extreme protection for VIP/staff/restricted accounts.</summary>
    Extreme = 3,
}
