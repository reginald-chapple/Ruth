using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Courses;

internal sealed class GetLearningCoursesQueryHandler(LmsDbContext db)
    : IQueryHandler<GetLearningCoursesQuery, IReadOnlyList<LearningCourseResponse>>
{
    public async Task<Result<IReadOnlyList<LearningCourseResponse>>> HandleAsync(
        GetLearningCoursesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db
            .Courses.AsNoTracking()
            .Include(static c => c.Requirements)
            .Where(static c => c.IsActive);

        if (query.LearningProgramId is { } pid && pid != Guid.Empty)
        {
            var progId = LearningProgramId.From(pid);
            q = q.Where(c => c.LearningProgramId == progId);
        }

        if (query.PublicOnly)
        {
            q = q.Where(static c => c.IsPublic);
        }

        var items = await q.OrderBy(static c => c.Order)
            .ThenBy(static c => c.Name)
            .ToListAsync(cancellationToken);
        var names = items.ToDictionary(static c => c.Id, static c => c.Name);
        return Result.Success<IReadOnlyList<LearningCourseResponse>>([
            .. items.Select(c => LmsDtoMapper.ToResponse(c, names)),
        ]);
    }
}
