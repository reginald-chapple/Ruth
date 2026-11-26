namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Channel through which a financial transaction originated.</summary>
public enum FinancialTransactionSourceType
{
    /// <summary>Public website or member portal checkout.</summary>
    Website = 0,

    /// <summary>Internal staff entry or back-office teller screen.</summary>
    StaffEntry = 1,

    /// <summary>Onsite touch check-in or giving kiosk.</summary>
    Kiosk = 2,

    /// <summary>Mobile application payment.</summary>
    Mobile = 3,

    /// <summary>MICR / optical bank check scanner ingestion.</summary>
    BankCheckScanner = 4,

    /// <summary>Automated recurring billing settlement engine.</summary>
    RecurringEngine = 5,
}
