using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Schedules;

internal sealed class CreateScheduleCommandHandler(CoreDbContext dbContext, IClock clock)
    : ICommandHandler<CreateScheduleCommand, ScheduleResponse>
{
    public async Task<Result<ScheduleResponse>> HandleAsync(
        CreateScheduleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = Schedule.Create(ScheduleMapper.ToInput(command));
        if (created.IsFailure)
        {
            return Result.Failure<ScheduleResponse>(created.Errors);
        }

        _ = dbContext.Schedules.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await ScheduleDateMaterializer.SyncScheduleDatesAsync(
            dbContext,
            created.Value,
            clock,
            command.TimeZoneId,
            cancellationToken
        );

        return Result.Success(ScheduleMapper.ToResponse(created.Value));
    }
}
