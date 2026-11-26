using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal sealed class ResumeRecurringScheduleCommandHandler(
    FinanceDbContext dbContext,
    IClock clock
) : ICommandHandler<ResumeRecurringScheduleCommand, RecurringScheduleResponse>
{
    public async Task<Result<RecurringScheduleResponse>> HandleAsync(
        ResumeRecurringScheduleCommand command,
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

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var nextDate = command.NextPaymentDate
            ?? (schedule.NextPaymentDate >= today ? schedule.NextPaymentDate : today);

        var resumed = schedule.Resume(nextDate);
        if (resumed.IsFailure)
        {
            return Result.Failure<RecurringScheduleResponse>(resumed.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return await RecurringScheduleMapper.HydrateResponseAsync(
            dbContext,
            schedule,
            cancellationToken
        );
    }
}
