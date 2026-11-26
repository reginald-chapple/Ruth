namespace Ruth.Modules.Finance.Contracts.Accounts;

/// <summary>Represents a currency-qualified monetary amount in ISO-4217 format.</summary>
/// <param name="Amount">The exact decimal monetary value.</param>
/// <param name="CurrencyCode">The 3-letter uppercase ISO-4217 currency code (for example, USD).</param>
public sealed record MoneyDto(decimal Amount, string CurrencyCode = "USD");
