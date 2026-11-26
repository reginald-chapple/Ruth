namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Supported payment instrument kinds for gateway tokenization and settlement.</summary>
public enum PaymentMethodKind
{
    /// <summary>Credit or debit card instrument.</summary>
    CreditCard = 0,

    /// <summary>ACH / EFT bank account instrument.</summary>
    AchBankAccount = 1,
}
