using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Contracts.Events;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Courses;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Courses;

internal sealed class CreateLearningCourseCommandHandler(LmsDbContext db)
    : ICommandHandler<CreateLearningCourseCommand, LearningCourseResponse>
{
    public async Task<Result<LearningCourseResponse>> HandleAsync(
        CreateLearningCourseCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var programId = LearningProgramId.From(command.LearningProgramId);
        var preCheck = await ValidateUniquenessAsync(
            programId,
            command.CourseCode,
            cancellationToken
        );
        if (preCheck.IsFailure)
        {
            return Result.Failure<LearningCourseResponse>(preCheck.PrimaryError);
        }

        var created = LearningCourse.Create(
            programId,
            command.Name,
            command.CourseCode,
            command.PublicName,
            command.Summary,
            command.Description,
            command.Credits,
            command.MaxStudents,
            command.AllowHistoricalAccess,
            command.EnableAnnouncements,
            command.IsPublic,
            command.Order
        );
        if (created.IsFailure)
        {
            return Result.Failure<LearningCourseResponse>(created.PrimaryError);
        }

        _ = db.Courses.Add(created.Value);
        db.EnqueueIntegrationEvent(
            new LearningCoursePublishedIntegrationEvent(
                created.Value.Id.Value,
                created.Value.LearningProgramId.Value,
                created.Value.CourseCode,
                created.Value.PublicName,
                created.Value.Summary,
                created.Value.IsPublic
            )
        );
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(LmsDtoMapper.ToResponse(created.Value));
    }

    private async Task<Result> ValidateUniquenessAsync(
        LearningProgramId programId,
        string courseCode,
        CancellationToken ct
    )
    {
        if (!await db.Programs.AsNoTracking().AnyAsync(p => p.Id == programId, ct))
        {
            return Result.Failure(
                LmsErrors.NotFound("Program.NotFound", "LearningProgram was not found.")
            );
        }

        var normalizedCode = courseCode.Trim().ToUpperInvariant();
        return await db
            .Courses.AsNoTracking()
            .AnyAsync(c => c.LearningProgramId == programId && c.CourseCode == normalizedCode, ct)
            ? Result.Failure(
                LmsErrors.Conflict(
                    "Course.DuplicateCode",
                    $"Course code '{normalizedCode}' already exists in this program."
                )
            )
            : Result.Success();
    }
}
