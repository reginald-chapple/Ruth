using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Schedules;

internal sealed class DeleteScheduleCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeleteScheduleCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteScheduleCommand command,
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

        var dates = await dbContext
            .ScheduleDates.Where(d => d.ScheduleId == id)
            .ToListAsync(cancellationToken);
        dbContext.ScheduleDates.RemoveRange(dates);
        _ = dbContext.Schedules.Remove(schedule);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
