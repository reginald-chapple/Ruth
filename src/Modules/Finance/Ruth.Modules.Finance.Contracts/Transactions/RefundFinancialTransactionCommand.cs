using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Command to process a full or partial refund on a settled financial transaction.</summary>
/// <param name="TransactionId">Target financial transaction identifier.</param>
/// <param name="Amount">Positive refund amount (cannot exceed remaining net settled amount).</param>
/// <param name="Reason">Audit reason for the refund.</param>
/// <param name="ProcessedByPersonId">Optional staff Person identifier processing the refund.</param>
/// <param name="ExecuteGatewayRefund">When true and the transaction has a gateway reference, invokes IPaymentGateway.RefundAsync.</param>
public sealed record RefundFinancialTransactionCommand(
    Guid TransactionId,
    decimal Amount,
    string Reason,
    Guid? ProcessedByPersonId = null,
    bool ExecuteGatewayRefund = true
) : ICommand<FinancialTransactionResponse>;
