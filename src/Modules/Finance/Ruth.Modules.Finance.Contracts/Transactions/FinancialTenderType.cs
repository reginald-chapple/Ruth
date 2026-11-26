namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Payment tender instrument used on a financial transaction.</summary>
public enum FinancialTenderType
{
    /// <summary>Credit or debit card.</summary>
    CreditCard = 0,

    /// <summary>ACH / EFT bank transfer.</summary>
    Ach = 1,

    /// <summary>Paper or e-check.</summary>
    Check = 2,

    /// <summary>Physical cash currency.</summary>
    Cash = 3,

    /// <summary>Bank wire transfer.</summary>
    Wire = 4,

    /// <summary>Institutional grant or foundation disbursement.</summary>
    ExternalGrant = 5,
}
