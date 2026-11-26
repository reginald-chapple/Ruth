using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Classes;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Lms.Slices.Classes;

internal sealed class SubmitLearningActivityCommandHandler(
    LmsDbContext db,
    LmsProgressRecalculator recalculator,
    IClock clock
) : ICommandHandler<SubmitLearningActivityCommand, LearningActivityCompletionResponse>
{
    public async Task<Result<LearningActivityCompletionResponse>> HandleAsync(
        SubmitLearningActivityCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var classId = LearningClassId.From(command.LearningClassId);
        var activityId = LearningActivityId.From(command.LearningActivityId);

        var cls = await db
            .Classes.AsNoTracking()
            .Include(static c => c.Activities)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        var activity = cls?.Activities.FirstOrDefault(a => a.Id == activityId);
        if (cls is null || activity is null)
        {
            return Result.Failure<LearningActivityCompletionResponse>(
                LmsErrors.NotFound("Activity.NotFound", "LearningActivity was not found.")
            );
        }

        var participant = await db.Participants.FirstOrDefaultAsync(
            p => p.LearningClassId == classId && p.PersonId == command.PersonId,
            cancellationToken
        );
        if (participant is null)
        {
            return Result.Failure<LearningActivityCompletionResponse>(
                LmsErrors.NotFound(
                    "Participant.NotFound",
                    "Learner is not enrolled in this LearningClass."
                )
            );
        }

        var completion = await UpsertCompletionAsync(
            cls,
            activity,
            participant,
            command,
            cancellationToken
        );
        _ = await db.SaveChangesAsync(cancellationToken);
        await recalculator.RecalculateAndStageCompletionAsync(cls, participant, cancellationToken);
        _ = await db.SaveChangesAsync(cancellationToken);

        return Result.Success(LmsDtoMapper.ToResponse(completion, activity));
    }

    private async Task<LearningActivityCompletion> UpsertCompletionAsync(
        LearningClass cls,
        LearningActivity activity,
        LearningParticipant participant,
        SubmitLearningActivityCommand command,
        CancellationToken ct
    )
    {
        var existing = await db.ActivityCompletions.FirstOrDefaultAsync(
            c => c.LearningParticipantId == participant.Id && c.LearningActivityId == activity.Id,
            ct
        );
        var nowUtc = clock.UtcNow;
        if (existing is not null)
        {
            existing.ApplyResubmission(
                activity,
                nowUtc,
                command.SubmissionDataJson,
                command.BinaryFileId,
                command.StudentComment
            );
            return existing;
        }

        var termStart = cls.LearningTermId is { } tid
            ? await db
                .Terms.AsNoTracking()
                .Where(t => t.Id == tid)
                .Select(static t => t.StartDate)
                .FirstOrDefaultAsync(ct)
            : null;
        var dueDate = activity.CalculateDueDateUtc(participant.EnrolledDateTimeUtc, termStart);
        var created = LearningActivityCompletion.Submit(
            activity,
            participant,
            dueDate,
            nowUtc,
            command.SubmissionDataJson,
            command.BinaryFileId,
            command.StudentComment
        );
        _ = db.ActivityCompletions.Add(created);
        return created;
    }
}
