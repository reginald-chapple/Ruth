using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.PaymentMethods;

/// <summary>Query to match a scanned check or ACH routing/account pair to a constituent Person via keyed HMAC-SHA256 lookup.</summary>
/// <param name="RoutingNumber">9-digit ABA routing transit number.</param>
/// <param name="AccountNumber">Bank account number.</param>
public sealed record ResolvePersonByBankAccountQuery(string RoutingNumber, string AccountNumber)
    : IQuery<BankAccountIdentifierResponse>;
