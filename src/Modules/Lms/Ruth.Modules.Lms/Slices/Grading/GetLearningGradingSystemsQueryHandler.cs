using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Grading;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Grading;

internal sealed class GetLearningGradingSystemsQueryHandler(LmsDbContext db)
    : IQueryHandler<GetLearningGradingSystemsQuery, IReadOnlyList<LearningGradingSystemResponse>>
{
    public async Task<Result<IReadOnlyList<LearningGradingSystemResponse>>> HandleAsync(
        GetLearningGradingSystemsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.GradingSystems.AsNoTracking().Include(static g => g.Scales).AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static g => g.IsActive);
        }

        var items = await q.OrderBy(static g => g.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LearningGradingSystemResponse>>([
            .. items.Select(LmsDtoMapper.ToResponse),
        ]);
    }
}
