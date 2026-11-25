using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Schedules;

internal sealed class GetScheduleByIdQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetScheduleByIdQuery, ScheduleResponse>
{
    public async Task<Result<ScheduleResponse>> HandleAsync(
        GetScheduleByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = ScheduleId.From(query.ScheduleId);
        var schedule = await dbContext
            .Schedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return schedule is null
            ? CoreScheduleErrors.ScheduleNotFound
            : Result.Success(ScheduleMapper.ToResponse(schedule));
    }
}
