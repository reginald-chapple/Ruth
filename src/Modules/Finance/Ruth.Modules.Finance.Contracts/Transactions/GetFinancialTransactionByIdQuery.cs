using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Query to retrieve a single financial transaction by identifier.</summary>
/// <param name="TransactionId">Target financial transaction identifier.</param>
public sealed record GetFinancialTransactionByIdQuery(Guid TransactionId)
    : IQuery<FinancialTransactionResponse>;
