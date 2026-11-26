using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Classes;

internal sealed class GetFacilitatorGradingQueueQueryHandler(LmsDbContext db)
    : IQueryHandler<
        GetFacilitatorGradingQueueQuery,
        IReadOnlyList<LearningActivityCompletionResponse>
    >
{
    public async Task<Result<IReadOnlyList<LearningActivityCompletionResponse>>> HandleAsync(
        GetFacilitatorGradingQueueQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db
            .ActivityCompletions.AsNoTracking()
            .Where(static c => c.IsStudentCompleted && c.RequiresGrading);
        if (query.LearningClassId is { } cid && cid != Guid.Empty)
        {
            var classId = LearningClassId.From(cid);
            q = q.Where(c => c.LearningClassId == classId);
        }

        var pending = await q.OrderBy(static c => c.CompletedDateTimeUtc)
            .ToListAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return Result.Success<IReadOnlyList<LearningActivityCompletionResponse>>([]);
        }

        var activityIds = pending.Select(static p => p.LearningActivityId).Distinct().ToList();
        var activities = await db
            .Activities.AsNoTracking()
            .Where(a => activityIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, cancellationToken);

        List<LearningActivityCompletionResponse> list = [];
        foreach (var comp in pending)
        {
            if (activities.TryGetValue(comp.LearningActivityId, out var act))
            {
                list.Add(LmsDtoMapper.ToResponse(comp, act));
            }
        }

        return Result.Success<IReadOnlyList<LearningActivityCompletionResponse>>(list);
    }
}
