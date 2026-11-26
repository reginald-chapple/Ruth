using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.PaymentMethods;

/// <summary>Command to validate a 9-digit ABA routing number and account number, hash them, and bind the digest to a Person.</summary>
/// <param name="PersonId">Target constituent or Business organization Person identifier.</param>
/// <param name="RoutingNumber">9-digit ABA routing transit number (never persisted in plaintext).</param>
/// <param name="AccountNumber">Bank account number (never persisted in plaintext).</param>
/// <param name="BankName">Optional bank display name.</param>
public sealed record RegisterBankAccountIdentifierCommand(
    Guid PersonId,
    string RoutingNumber,
    string AccountNumber,
    string? BankName = null
) : ICommand<BankAccountIdentifierResponse>;
