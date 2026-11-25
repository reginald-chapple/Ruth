using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Schedules;

internal sealed class GetScheduleCategoryExclusionsQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<
        GetScheduleCategoryExclusionsQuery,
        IReadOnlyList<ScheduleCategoryExclusionResponse>
    >
{
    public async Task<Result<IReadOnlyList<ScheduleCategoryExclusionResponse>>> HandleAsync(
        GetScheduleCategoryExclusionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var categoryId = CategoryId.From(query.CategoryId);
        var items = await dbContext
            .ScheduleCategoryExclusions.AsNoTracking()
            .Where(e => e.CategoryId == categoryId)
            .OrderBy(static e => e.StartDate)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ScheduleCategoryExclusionResponse>>([
            .. items.Select(ScheduleMapper.ToResponse),
        ]);
    }
}
