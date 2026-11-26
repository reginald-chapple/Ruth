namespace Ruth.Modules.Finance.Contracts.Accounts;

/// <summary>Classifies the operational purpose of a financial account across charitable and commercial ledgers.</summary>
public enum FinancialAccountKind
{
    /// <summary>Tax-deductible charitable contribution fund.</summary>
    CharitableFund = 0,

    /// <summary>Standard membership dues or chapter subscription revenue account.</summary>
    MembershipDues = 1,

    /// <summary>Program, class, event, or service fee revenue account.</summary>
    ProgramFee = 2,

    /// <summary>Restricted or unrestricted grant fund.</summary>
    GrantFund = 3,

    /// <summary>General operating, rental, or merchandise revenue account.</summary>
    OperatingRevenue = 4,
}
