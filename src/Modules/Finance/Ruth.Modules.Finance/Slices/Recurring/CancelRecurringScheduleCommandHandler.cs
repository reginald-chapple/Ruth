using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal sealed class CancelRecurringScheduleCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<CancelRecurringScheduleCommand, RecurringScheduleResponse>
{
    public async Task<Result<RecurringScheduleResponse>> HandleAsync(
        CancelRecurringScheduleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var scheduleId = RecurringScheduleTransactionId.From(command.ScheduleId);
        var schedule = await dbContext
            .RecurringSchedules.Include(static s => s.Splits)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null)
        {
            return FinanceErrors.RecurringScheduleNotFound;
        }

        var cancelled = schedule.Cancel(command.Reason);
        if (cancelled.IsFailure)
        {
            return Result.Failure<RecurringScheduleResponse>(cancelled.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return await RecurringScheduleMapper.HydrateResponseAsync(
            dbContext,
            schedule,
            cancellationToken
        );
    }
}
