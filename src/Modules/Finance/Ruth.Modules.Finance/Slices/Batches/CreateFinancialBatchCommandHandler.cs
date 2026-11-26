using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Batches;

internal sealed class CreateFinancialBatchCommandHandler(FinanceDbContext dbContext, IClock clock)
    : ICommandHandler<CreateFinancialBatchCommand, FinancialBatchResponse>
{
    public async Task<Result<FinancialBatchResponse>> HandleAsync(
        CreateFinancialBatchCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = FinancialBatch.Create(
            command.Name,
            command.CurrencyCode,
            command.ControlAmount,
            command.ControlItemCount,
            command.BatchStartDateTimeUtc ?? clock.UtcNow,
            command.BranchId,
            command.AccountingSystemCode,
            isAutomated: false,
            command.OpenImmediately
        );
        if (created.IsFailure)
        {
            return Result.Failure<FinancialBatchResponse>(created.Errors);
        }

        _ = dbContext.FinancialBatches.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return FinancialBatchMapper.ToResponse(created.Value, actualAmount: 0m, actualCount: 0);
    }
}
