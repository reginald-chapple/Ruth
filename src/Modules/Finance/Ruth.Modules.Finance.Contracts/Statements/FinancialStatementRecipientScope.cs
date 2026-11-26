namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Identifies the constituent grouping scope for a generated tax receipt or account statement.</summary>
public enum FinancialStatementRecipientScope
{
    /// <summary>Single individual constituent record.</summary>
    Individual = 0,

    /// <summary>Combined household or billing unit aggregating eligible household members.</summary>
    Household = 1,

    /// <summary>Corporate sponsor, grantor, or organization record.</summary>
    OrganizationSponsor = 2,
}
