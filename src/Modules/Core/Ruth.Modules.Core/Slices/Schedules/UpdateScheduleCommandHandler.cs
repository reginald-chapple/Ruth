using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Schedules;

internal sealed class UpdateScheduleCommandHandler(CoreDbContext dbContext, IClock clock)
    : ICommandHandler<UpdateScheduleCommand, ScheduleResponse>
{
    public async Task<Result<ScheduleResponse>> HandleAsync(
        UpdateScheduleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = ScheduleId.From(command.ScheduleId);
        var schedule = await dbContext.Schedules.FirstOrDefaultAsync(
            s => s.Id == id,
            cancellationToken
        );
        if (schedule is null)
        {
            return CoreScheduleErrors.ScheduleNotFound;
        }

        var updated = schedule.Update(ScheduleMapper.ToInput(command));
        if (updated.IsFailure)
        {
            return Result.Failure<ScheduleResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await ScheduleDateMaterializer.SyncScheduleDatesAsync(
            dbContext,
            schedule,
            clock,
            command.TimeZoneId,
            cancellationToken
        );

        return Result.Success(ScheduleMapper.ToResponse(schedule));
    }
}
