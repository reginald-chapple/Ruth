namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Operational purpose of a recurring schedule across charitable and commercial ledgers.</summary>
public enum RecurringSchedulePurpose
{
    /// <summary>Recurring charitable gift or fund pledge installment.</summary>
    CharitableContribution = 0,

    /// <summary>Recurring membership dues or chapter subscription.</summary>
    MembershipSubscription = 1,

    /// <summary>Recurring program, coaching, or service retainer.</summary>
    ProgramRetainer = 2,
}
