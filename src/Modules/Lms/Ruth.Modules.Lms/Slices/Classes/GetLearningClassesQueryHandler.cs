using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Classes;

internal sealed class GetLearningClassesQueryHandler(LmsDbContext db)
    : IQueryHandler<GetLearningClassesQuery, IReadOnlyList<LearningClassResponse>>
{
    public async Task<Result<IReadOnlyList<LearningClassResponse>>> HandleAsync(
        GetLearningClassesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db
            .Classes.AsNoTracking()
            .Include(static c => c.ContentPages)
            .Include(static c => c.Announcements)
            .Include(static c => c.Activities)
            .Where(static c => c.IsActive);

        if (query.LearningCourseId is { } cid && cid != Guid.Empty)
        {
            var courseId = LearningCourseId.From(cid);
            q = q.Where(c => c.LearningCourseId == courseId);
        }

        var classes = await q.OrderBy(static c => c.Name).ToListAsync(cancellationToken);
        var classIds = classes.Select(static c => c.Id).ToList();
        var counts = await db
            .Participants.AsNoTracking()
            .Where(p =>
                classIds.Contains(p.LearningClassId) && p.Role == LearningParticipantRole.Student
            )
            .GroupBy(static p => p.LearningClassId)
            .ToDictionaryAsync(static g => g.Key, static g => g.Count(), cancellationToken);

        return Result.Success<IReadOnlyList<LearningClassResponse>>([
            .. classes.Select(c => LmsDtoMapper.ToResponse(c, counts.GetValueOrDefault(c.Id))),
        ]);
    }
}
