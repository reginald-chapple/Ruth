using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Portal;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Classes;
using Ruth.Modules.Lms.Domain.Courses;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Portal;

internal sealed class GetPublicCourseCatalogQueryHandler(LmsDbContext db)
    : IQueryHandler<GetPublicCourseCatalogQuery, IReadOnlyList<PublicCourseCatalogItemResponse>>
{
    public async Task<Result<IReadOnlyList<PublicCourseCatalogItemResponse>>> HandleAsync(
        GetPublicCourseCatalogQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var programs = await db
            .Programs.AsNoTracking()
            .Where(static p => p.IsActive && p.IsPublic)
            .ToDictionaryAsync(static p => p.Id, cancellationToken);

        var courseQuery = db
            .Courses.AsNoTracking()
            .Include(static c => c.Requirements)
            .Where(static c => c.IsActive && c.IsPublic);
        if (query.ProgramId is { } pid && pid != Guid.Empty)
        {
            var progId = LearningProgramId.From(pid);
            courseQuery = courseQuery.Where(c => c.LearningProgramId == progId);
        }

        var courses = await courseQuery
            .OrderBy(static c => c.Order)
            .ThenBy(static c => c.PublicName)
            .ToListAsync(cancellationToken);
        var courseIds = courses.Select(static c => c.Id).ToList();
        var courseNames = courses.ToDictionary(static c => c.Id, static c => c.PublicName);

        var classes = await db
            .Classes.AsNoTracking()
            .Include(static c => c.Activities)
            .Where(c => courseIds.Contains(c.LearningCourseId) && c.IsActive && c.IsPublic)
            .ToListAsync(cancellationToken);

        var (passedCourses, enrollmentByCourse) = await LoadLearnerContextAsync(
            query.PersonId,
            classes,
            cancellationToken
        );

        var studentCounts = await db
            .Participants.AsNoTracking()
            .Where(static p => p.Role == LearningParticipantRole.Student)
            .GroupBy(static p => p.LearningClassId)
            .ToDictionaryAsync(static g => g.Key, static g => g.Count(), cancellationToken);

        return Result.Success<IReadOnlyList<PublicCourseCatalogItemResponse>>(
            BuildCatalogItems(
                programs,
                courses,
                classes,
                studentCounts,
                passedCourses,
                enrollmentByCourse
            )
        );
    }

    private static List<PublicCourseCatalogItemResponse> BuildCatalogItems(
        Dictionary<LearningProgramId, Domain.Programs.LearningProgram> programs,
        List<LearningCourse> courses,
        List<LearningClass> classes,
        Dictionary<LearningClassId, int> studentCounts,
        HashSet<LearningCourseId> passedCourses,
        Dictionary<LearningCourseId, LearningParticipantResponse> enrollmentByCourse
    )
    {
        var courseNames = courses.ToDictionary(static c => c.Id, static c => c.PublicName);
        List<PublicCourseCatalogItemResponse> catalog = [];
        foreach (var course in courses)
        {
            if (!programs.TryGetValue(course.LearningProgramId, out var program))
            {
                continue;
            }

            List<LearningClassResponse> openClasses =
            [
                .. classes
                    .Where(c => c.LearningCourseId == course.Id)
                    .Select(c => LmsDtoMapper.ToResponse(c, studentCounts.GetValueOrDefault(c.Id))),
            ];
            var prereqsMet = CoursePrerequisiteEvaluator.ArePrerequisitesMet(course, passedCourses);
            _ = enrollmentByCourse.TryGetValue(course.Id, out var enrollment);
            catalog.Add(
                new PublicCourseCatalogItemResponse(
                    LmsDtoMapper.ToResponse(program),
                    LmsDtoMapper.ToResponse(course, courseNames),
                    openClasses,
                    prereqsMet,
                    enrollment
                )
            );
        }

        return catalog;
    }

    private async Task<(
        HashSet<LearningCourseId> PassedCourses,
        Dictionary<LearningCourseId, LearningParticipantResponse> EnrollmentByCourse
    )> LoadLearnerContextAsync(
        Guid? personId,
        List<LearningClass> openClasses,
        CancellationToken ct
    )
    {
        var passedCourses = new HashSet<LearningCourseId>();
        var enrollmentByCourse = new Dictionary<LearningCourseId, LearningParticipantResponse>();
        if (personId is not { } pid || pid == Guid.Empty)
        {
            return (passedCourses, enrollmentByCourse);
        }

        var enrollments = await db
            .Participants.AsNoTracking()
            .Where(p => p.PersonId == pid)
            .ToListAsync(ct);
        var classToCourse = await db
            .Classes.AsNoTracking()
            .ToDictionaryAsync(static c => c.Id, static c => c.LearningCourseId, ct);

        foreach (var e in enrollments)
        {
            if (!classToCourse.TryGetValue(e.LearningClassId, out var cid))
            {
                continue;
            }

            if (e.LearningCompletionStatus == LearningCompletionStatus.Pass)
            {
                _ = passedCourses.Add(cid);
            }

            var cls = openClasses.Find(c => c.Id == e.LearningClassId);
            var totalActs = cls?.Activities.Count(static a =>
                a.AssignTo == LearningActivityAssignTo.Student
            ) ?? 0;
            enrollmentByCourse[cid] = LmsDtoMapper.ToResponse(e, completedCount: 0, totalActs);
        }

        return (passedCourses, enrollmentByCourse);
    }
}
