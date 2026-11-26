using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Batches;

/// <summary>Command to create a new financial settlement batch.</summary>
/// <param name="Name">Batch display name.</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="ControlAmount">Expected control total amount (non-negative).</param>
/// <param name="ControlItemCount">Optional expected transaction count (0 when unconstrained).</param>
/// <param name="BatchStartDateTimeUtc">Optional batch start timestamp (defaults to UTC now).</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="AccountingSystemCode">Optional external accounting reference.</param>
/// <param name="OpenImmediately">When true, transitions directly from Pending to Open.</param>
public sealed record CreateFinancialBatchCommand(
    string Name,
    string CurrencyCode = "USD",
    decimal ControlAmount = 0m,
    int ControlItemCount = 0,
    DateTimeOffset? BatchStartDateTimeUtc = null,
    Guid? BranchId = null,
    string? AccountingSystemCode = null,
    bool OpenImmediately = false
) : ICommand<FinancialBatchResponse>;
