using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Programs;

internal sealed class GetLearningProgramsQueryHandler(LmsDbContext db)
    : IQueryHandler<GetLearningProgramsQuery, IReadOnlyList<LearningProgramResponse>>
{
    public async Task<Result<IReadOnlyList<LearningProgramResponse>>> HandleAsync(
        GetLearningProgramsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.Programs.AsNoTracking();
        if (!query.IncludeInactive)
        {
            q = q.Where(static p => p.IsActive);
        }

        if (query.PublicOnly)
        {
            q = q.Where(static p => p.IsPublic);
        }

        var items = await q.OrderBy(static p => p.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LearningProgramResponse>>([
            .. items.Select(LmsDtoMapper.ToResponse),
        ]);
    }
}
