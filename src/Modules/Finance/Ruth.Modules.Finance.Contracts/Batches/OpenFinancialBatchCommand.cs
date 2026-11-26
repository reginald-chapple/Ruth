using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Batches;

/// <summary>Command to transition a Pending financial batch to Open and optionally update control targets.</summary>
/// <param name="BatchId">Target batch identifier.</param>
/// <param name="ControlAmount">Optional updated control amount.</param>
/// <param name="ControlItemCount">Optional updated control item count.</param>
public sealed record OpenFinancialBatchCommand(
    Guid BatchId,
    decimal? ControlAmount = null,
    int? ControlItemCount = null
) : ICommand<FinancialBatchResponse>;
