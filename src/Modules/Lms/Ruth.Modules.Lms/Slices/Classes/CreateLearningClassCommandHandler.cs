using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Classes;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Classes;

internal sealed class CreateLearningClassCommandHandler(LmsDbContext db)
    : ICommandHandler<CreateLearningClassCommand, LearningClassResponse>
{
    public async Task<Result<LearningClassResponse>> HandleAsync(
        CreateLearningClassCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var courseId = LearningCourseId.From(command.LearningCourseId);
        var course = await db
            .Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
        if (course is null)
        {
            return Result.Failure<LearningClassResponse>(
                LmsErrors.NotFound("Course.NotFound", "LearningCourse was not found.")
            );
        }

        LearningTermId? termId =
            command.LearningTermId is { } tid && tid != Guid.Empty
                ? LearningTermId.From(tid)
                : null;
        var gradingId =
            command.LearningGradingSystemId is { } gid && gid != Guid.Empty
                ? LearningGradingSystemId.From(gid)
                : await db
                    .Programs.AsNoTracking()
                    .Where(p => p.Id == course.LearningProgramId)
                    .Select(static p => p.DefaultGradingSystemId)
                    .FirstOrDefaultAsync(cancellationToken);

        var created = LearningClass.Create(
            courseId,
            command.Name,
            termId,
            gradingId,
            command.BranchId,
            command.VirtualRoomId,
            command.IsPublic,
            command.TakesAttendance,
            command.AllowOverbooking,
            command.MaxStudents ?? course.MaxStudents
        );
        if (created.IsFailure)
        {
            return Result.Failure<LearningClassResponse>(created.PrimaryError);
        }

        _ = db.Classes.Add(created.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(LmsDtoMapper.ToResponse(created.Value, enrolledStudentCount: 0));
    }
}
