using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Lms.Slices.Classes;

internal sealed class GradeLearningActivityCommandHandler(
    LmsDbContext db,
    LmsProgressRecalculator recalculator,
    IClock clock
) : ICommandHandler<GradeLearningActivityCommand, LearningActivityCompletionResponse>
{
    public async Task<Result<LearningActivityCompletionResponse>> HandleAsync(
        GradeLearningActivityCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var compId = LearningActivityCompletionId.From(command.CompletionId);
        var completion = await db.ActivityCompletions.FirstOrDefaultAsync(
            c => c.Id == compId,
            cancellationToken
        );
        if (completion is null)
        {
            return Result.Failure<LearningActivityCompletionResponse>(
                LmsErrors.NotFound(
                    "Completion.NotFound",
                    "Activity completion submission was not found."
                )
            );
        }

        var cls = await db
            .Classes.AsNoTracking()
            .Include(static c => c.Activities)
            .FirstOrDefaultAsync(c => c.Id == completion.LearningClassId, cancellationToken);
        var activity = cls?.Activities.FirstOrDefault(a => a.Id == completion.LearningActivityId);
        if (cls is null || activity is null)
        {
            return Result.Failure<LearningActivityCompletionResponse>(
                LmsErrors.NotFound("Activity.NotFound", "Owning activity was not found.")
            );
        }

        var gradeRes = completion.Grade(
            command.PointsEarned,
            activity.Points,
            command.FacilitatorPersonId,
            command.FacilitatorComment,
            clock.UtcNow
        );
        if (gradeRes.IsFailure)
        {
            return Result.Failure<LearningActivityCompletionResponse>(gradeRes.PrimaryError);
        }

        _ = await db.SaveChangesAsync(cancellationToken);

        var participant = await db.Participants.FirstOrDefaultAsync(
            p => p.Id == completion.LearningParticipantId,
            cancellationToken
        );
        if (participant is not null)
        {
            await recalculator.RecalculateAndStageCompletionAsync(
                cls,
                participant,
                cancellationToken
            );
            _ = await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(LmsDtoMapper.ToResponse(completion, activity));
    }
}
