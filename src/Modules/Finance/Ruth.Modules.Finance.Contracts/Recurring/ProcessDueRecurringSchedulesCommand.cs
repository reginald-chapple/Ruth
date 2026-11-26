using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Command to process all active recurring schedules due on or before AsOfDate.</summary>
/// <param name="AsOfDate">Optional cutoff date (defaults to UTC today).</param>
/// <param name="MaxBatchSize">Maximum due schedules to process in one pass.</param>
public sealed record ProcessDueRecurringSchedulesCommand(
    DateOnly? AsOfDate = null,
    int MaxBatchSize = 200
) : ICommand<RecurringSettlementRunResponse>;
