using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Courses;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Courses;

internal sealed class AddCourseRequirementCommandHandler(LmsDbContext db)
    : ICommandHandler<AddCourseRequirementCommand, LearningCourseResponse>
{
    public async Task<Result<LearningCourseResponse>> HandleAsync(
        AddCourseRequirementCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var courseId = LearningCourseId.From(command.LearningCourseId);
        var requiredId = LearningCourseId.From(command.RequiredLearningCourseId);

        var course = await db
            .Courses.Include(static c => c.Requirements)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
        if (course is null)
        {
            return Result.Failure<LearningCourseResponse>(
                LmsErrors.NotFound("Course.NotFound", "Target LearningCourse was not found.")
            );
        }

        var requiredExists = await db
            .Courses.AsNoTracking()
            .AnyAsync(c => c.Id == requiredId, cancellationToken);
        if (!requiredExists)
        {
            return Result.Failure<LearningCourseResponse>(
                LmsErrors.NotFound("Course.RequiredNotFound", "Required course was not found.")
            );
        }

        if (command.RequirementType == CourseRequirementType.Prerequisite)
        {
            var allReqs = await db
                .CourseRequirements.AsNoTracking()
                .Where(static r => r.RequirementType == CourseRequirementType.Prerequisite)
                .ToListAsync(cancellationToken);
            var graph = allReqs
                .GroupBy(static r => r.LearningCourseId)
                .ToDictionary(
                    static g => g.Key,
                    static g =>
                        (IReadOnlyList<LearningCourseId>)
                            [.. g.Select(static x => x.RequiredLearningCourseId)]
                );
            if (CoursePrerequisiteEvaluator.WouldCreateCycle(courseId, requiredId, graph))
            {
                return Result.Failure<LearningCourseResponse>(
                    LmsErrors.Validation(
                        "CourseRequirement.CycleDetected",
                        "Adding this prerequisite would create a circular dependency."
                    )
                );
            }
        }

        var addRes = course.AddRequirement(requiredId, command.RequirementType);
        if (addRes.IsFailure)
        {
            return Result.Failure<LearningCourseResponse>(addRes.PrimaryError);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
        var names = await db
            .Courses.AsNoTracking()
            .ToDictionaryAsync(static c => c.Id, static c => c.Name, cancellationToken);
        return Result.Success(LmsDtoMapper.ToResponse(course, names));
    }
}
