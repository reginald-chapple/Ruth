using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Query to list financial transactions filtered optionally by batch, person, account, or date range.</summary>
/// <param name="BatchId">Optional filter by financial batch identifier.</param>
/// <param name="PersonId">Optional filter by constituent or organization Person identifier.</param>
/// <param name="AccountId">Optional filter by split financial account identifier.</param>
/// <param name="FromUtc">Optional inclusive start UTC timestamp.</param>
/// <param name="ToUtc">Optional inclusive end UTC timestamp.</param>
/// <param name="MaxResults">Maximum transactions to return (1..500).</param>
public sealed record GetFinancialTransactionsQuery(
    Guid? BatchId = null,
    Guid? PersonId = null,
    Guid? AccountId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    int MaxResults = 100
) : IQuery<IReadOnlyList<FinancialTransactionResponse>>;
