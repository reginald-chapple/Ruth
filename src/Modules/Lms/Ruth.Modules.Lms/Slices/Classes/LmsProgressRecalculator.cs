using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Events;
using Ruth.Modules.Lms.Domain.Classes;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Lms.Slices.Classes;

internal sealed class LmsProgressRecalculator(LmsDbContext db, IClock clock)
{
    public async Task RecalculateAndStageCompletionAsync(
        LearningClass cls,
        LearningParticipant participant,
        CancellationToken ct
    )
    {
        var completions = await db
            .ActivityCompletions.Where(c => c.LearningParticipantId == participant.Id)
            .ToListAsync(ct);
        var gradingSystem = cls.LearningGradingSystemId is { } gid
            ? await db
                .GradingSystems.AsNoTracking()
                .Include(static g => g.Scales)
                .FirstOrDefaultAsync(g => g.Id == gid, ct)
            : null;

        var newlyPassed = participant.RecalculateProgress(
            cls.Activities,
            completions,
            gradingSystem,
            clock.UtcNow
        );
        if (!newlyPassed)
        {
            return;
        }

        var course = await db
            .Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cls.LearningCourseId, ct);
        if (course is null)
        {
            return;
        }

        db.EnqueueIntegrationEvent(
            new LearningCourseCompletedIntegrationEvent(
                participant.Id.Value,
                participant.PersonId,
                course.LearningProgramId.Value,
                course.Id.Value,
                cls.Id.Value,
                course.PublicName,
                participant.CurrentGradePercent,
                participant.CurrentGradeScaleName,
                participant.LearningCompletionDateTimeUtc ?? clock.UtcNow
            )
        );
    }
}
