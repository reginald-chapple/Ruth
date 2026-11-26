using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Domain.Batches;

namespace Ruth.Modules.Finance.Slices.Batches;

internal static class FinancialBatchMapper
{
    public static FinancialBatchResponse ToResponse(
        FinancialBatch batch,
        decimal actualAmount,
        int actualCount
    )
    {
        var variance = actualAmount - batch.ControlAmount;
        var countMatches = batch.ControlItemCount == 0 || actualCount == batch.ControlItemCount;

        return new FinancialBatchResponse(
            batch.Id.Value,
            batch.Name,
            batch.Status,
            batch.BranchId,
            batch.CurrencyCode,
            batch.ControlAmount,
            actualAmount,
            variance,
            batch.ControlItemCount,
            actualCount,
            variance == 0m && countMatches,
            batch.BatchStartDateTimeUtc,
            batch.BatchEndDateTimeUtc,
            batch.AccountingSystemCode,
            batch.IsAutomated,
            batch.ClosedByPersonId,
            batch.ClosedOnUtc,
            batch.AuditLockNote,
            batch.CreatedOnUtc
        );
    }
}
