using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Classes;
using Ruth.Modules.Lms.Domain.Courses;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Lms.Slices.Classes;

internal sealed class EnrollLearningParticipantCommandHandler(LmsDbContext db, IClock clock)
    : ICommandHandler<EnrollLearningParticipantCommand, LearningParticipantResponse>
{
    public async Task<Result<LearningParticipantResponse>> HandleAsync(
        EnrollLearningParticipantCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var classId = LearningClassId.From(command.LearningClassId);
        var cls = await db
            .Classes.AsNoTracking()
            .Include(static c => c.Activities)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (cls is null)
        {
            return Result.Failure<LearningParticipantResponse>(
                LmsErrors.NotFound("Class.NotFound", "LearningClass was not found.")
            );
        }

        var existing = await db
            .Participants.AsNoTracking()
            .AnyAsync(
                p => p.LearningClassId == classId && p.PersonId == command.PersonId,
                cancellationToken
            );
        if (existing)
        {
            return Result.Failure<LearningParticipantResponse>(
                LmsErrors.Conflict(
                    "Participant.AlreadyEnrolled",
                    "Person is already enrolled in this LearningClass."
                )
            );
        }

        var eligibility = await ValidateStudentEnrollmentAsync(cls, command, cancellationToken);
        if (eligibility.IsFailure)
        {
            return Result.Failure<LearningParticipantResponse>(eligibility.PrimaryError);
        }

        var participantRes = LearningParticipant.Enroll(
            classId,
            command.PersonId,
            command.Role,
            clock.UtcNow
        );
        if (participantRes.IsFailure)
        {
            return Result.Failure<LearningParticipantResponse>(participantRes.PrimaryError);
        }

        _ = db.Participants.Add(participantRes.Value);
        _ = await db.SaveChangesAsync(cancellationToken);

        var totalActivities = cls.Activities.Count(static a =>
            a.AssignTo == LearningActivityAssignTo.Student
        );
        return Result.Success(
            LmsDtoMapper.ToResponse(participantRes.Value, completedCount: 0, totalActivities)
        );
    }

    private async Task<Result> ValidateStudentEnrollmentAsync(
        LearningClass cls,
        EnrollLearningParticipantCommand command,
        CancellationToken ct
    )
    {
        if (command.Role != LearningParticipantRole.Student)
        {
            return Result.Success();
        }

        if (cls.MaxStudents is > 0 && !cls.AllowOverbooking)
        {
            var enrolled = await db
                .Participants.AsNoTracking()
                .CountAsync(
                    p => p.LearningClassId == cls.Id && p.Role == LearningParticipantRole.Student,
                    ct
                );
            if (enrolled >= cls.MaxStudents.Value)
            {
                return Result.Failure(
                    LmsErrors.Conflict("Class.Full", "LearningClass has reached maximum capacity.")
                );
            }
        }

        return command.BypassPrerequisites
            ? Result.Success()
            : await CheckPrerequisitesAsync(cls.LearningCourseId, command.PersonId, ct);
    }

    private async Task<Result> CheckPrerequisitesAsync(
        LearningCourseId courseId,
        Guid personId,
        CancellationToken ct
    )
    {
        var course = await db
            .Courses.AsNoTracking()
            .Include(static c => c.Requirements)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null || course.Requirements.Count == 0)
        {
            return Result.Success();
        }

        var passedClassIds = await db
            .Participants.AsNoTracking()
            .Where(p =>
                p.PersonId == personId
                && p.LearningCompletionStatus == LearningCompletionStatus.Pass
            )
            .Select(static p => p.LearningClassId)
            .ToListAsync(ct);
        var passedCourseIds = (
            await db
                .Classes.AsNoTracking()
                .Where(c => passedClassIds.Contains(c.Id))
                .Select(static c => c.LearningCourseId)
                .ToListAsync(ct)
        ).ToHashSet();

        return CoursePrerequisiteEvaluator.ArePrerequisitesMet(course, passedCourseIds)
            ? Result.Success()
            : Result.Failure(
                LmsErrors.Validation(
                    "Enrollment.PrerequisitesNotMet",
                    "Learner has not completed all prerequisite courses for this class."
                )
            );
    }
}
