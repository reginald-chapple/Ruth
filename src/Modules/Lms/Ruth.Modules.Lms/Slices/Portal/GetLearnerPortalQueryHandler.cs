using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Portal;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Portal;

internal sealed class GetLearnerPortalQueryHandler(LmsDbContext db)
    : IQueryHandler<GetLearnerPortalQuery, IReadOnlyList<LearnerClassWorkspaceResponse>>
{
    public async Task<Result<IReadOnlyList<LearnerClassWorkspaceResponse>>> HandleAsync(
        GetLearnerPortalQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var participants = await db
            .Participants.AsNoTracking()
            .Where(p => p.PersonId == query.PersonId)
            .OrderByDescending(static p => p.EnrolledDateTimeUtc)
            .ToListAsync(cancellationToken);
        if (participants.Count == 0)
        {
            return Result.Success<IReadOnlyList<LearnerClassWorkspaceResponse>>([]);
        }

        var classIds = participants.Select(static p => p.LearningClassId).Distinct().ToList();
        var classes = await db
            .Classes.AsNoTracking()
            .Include(static c => c.ContentPages)
            .Include(static c => c.Announcements)
            .Include(static c => c.Activities)
            .Where(c => classIds.Contains(c.Id))
            .ToDictionaryAsync(static c => c.Id, cancellationToken);

        var participantIds = participants.Select(static p => p.Id).ToList();
        var completions = await db
            .ActivityCompletions.AsNoTracking()
            .Where(c => participantIds.Contains(c.LearningParticipantId))
            .ToListAsync(cancellationToken);

        List<LearnerClassWorkspaceResponse> workspaces = [];
        foreach (var participant in participants)
        {
            if (classes.TryGetValue(participant.LearningClassId, out var cls))
            {
                workspaces.Add(BuildWorkspace(participant, cls, completions));
            }
        }

        return Result.Success<IReadOnlyList<LearnerClassWorkspaceResponse>>(workspaces);
    }

    private static LearnerClassWorkspaceResponse BuildWorkspace(
        Domain.Classes.LearningParticipant participant,
        Domain.Classes.LearningClass cls,
        List<Domain.Classes.LearningActivityCompletion> allCompletions
    )
    {
        var actById = cls.Activities.ToDictionary(static a => a.Id);
        var myComps = allCompletions.Where(c => c.LearningParticipantId == participant.Id).ToList();
        List<LearningActivityCompletionResponse> mapped =
        [
            .. myComps
                .Where(c => actById.ContainsKey(c.LearningActivityId))
                .Select(c => LmsDtoMapper.ToResponse(c, actById[c.LearningActivityId])),
        ];
        var totalStudentActs = cls.Activities.Count(static a =>
            a.AssignTo == LearningActivityAssignTo.Student
        );
        var completedCount = myComps.Count(static c => c.IsStudentCompleted && !c.RequiresGrading);
        return new LearnerClassWorkspaceResponse(
            LmsDtoMapper.ToResponse(participant, completedCount, totalStudentActs),
            LmsDtoMapper.ToResponse(cls, enrolledStudentCount: 0),
            mapped
        );
    }
}
