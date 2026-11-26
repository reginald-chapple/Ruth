using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Programs;

internal sealed class GetLearningTermsQueryHandler(LmsDbContext db)
    : IQueryHandler<GetLearningTermsQuery, IReadOnlyList<LearningTermResponse>>
{
    public async Task<Result<IReadOnlyList<LearningTermResponse>>> HandleAsync(
        GetLearningTermsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var programId = LearningProgramId.From(query.LearningProgramId);
        var items = await db
            .Terms.AsNoTracking()
            .Where(t => t.LearningProgramId == programId)
            .OrderBy(static t => t.StartDate)
            .ThenBy(static t => t.Name)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LearningTermResponse>>([
            .. items.Select(LmsDtoMapper.ToResponse),
        ]);
    }
}
