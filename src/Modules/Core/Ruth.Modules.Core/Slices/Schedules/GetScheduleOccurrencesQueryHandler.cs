using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Schedules;

internal sealed class GetScheduleOccurrencesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetScheduleOccurrencesQuery, IReadOnlyList<ScheduleOccurrenceDto>>
{
    public async Task<Result<IReadOnlyList<ScheduleOccurrenceDto>>> HandleAsync(
        GetScheduleOccurrencesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (query.RangeEndUtc < query.RangeStartUtc)
        {
            return CoreScheduleErrors.ScheduleRangeInvalid;
        }

        var id = ScheduleId.From(query.ScheduleId);
        var schedule = await dbContext
            .Schedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (schedule is null)
        {
            return CoreScheduleErrors.ScheduleNotFound;
        }

        var exclusions = await ScheduleDateMaterializer.LoadCategoryExclusionsAsync(
            dbContext,
            schedule.CategoryId,
            cancellationToken
        );

        var occurrences = schedule.GetOccurrences(
            query.RangeStartUtc,
            query.RangeEndUtc,
            query.TimeZoneId,
            exclusions
        );
        return Result.Success(occurrences);
    }
}
