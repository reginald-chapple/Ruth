using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Contracts.Grading;
using Ruth.Modules.Lms.Contracts.Portal;
using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Slices;

internal static class W6S7LmsEndpoints
{
    public static IEndpointRouteBuilder MapLmsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/lms").WithTags("LMS").RequireAuthorization();
        MapGradingSystems(admin);
        MapPrograms(admin);
        MapTerms(admin);
        MapCourses(admin);
        MapClasses(admin);
        MapClassContentAndActivities(admin);
        MapEnrollment(admin);
        MapGradingAndPortal(admin);
        MapPublicCatalog(endpoints);
        return endpoints;
    }

    private static void MapGradingSystems(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/grading-systems",
                static async (bool? includeInactive, IQueryDispatcher q, CancellationToken ct) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetLearningGradingSystemsQuery,
                            IReadOnlyList<LearningGradingSystemResponse>
                        >(new GetLearningGradingSystemsQuery(includeInactive == true), ct)
                    )
            )
            .WithName("GetLmsGradingSystems");

        _ = group
            .MapPost(
                "/grading-systems",
                static async (
                    CreateLearningGradingSystemCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateLearningGradingSystemCommand,
                        LearningGradingSystemResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/lms/grading-systems/{res.Value.Id:D}", res.Value)
                        : LmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateLmsGradingSystem");
    }

    private static void MapPrograms(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/programs",
                static async (
                    bool? publicOnly,
                    bool? includeInactive,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetLearningProgramsQuery,
                            IReadOnlyList<LearningProgramResponse>
                        >(
                            new GetLearningProgramsQuery(
                                publicOnly == true,
                                includeInactive == true
                            ),
                            ct
                        )
                    )
            )
            .WithName("GetLmsPrograms");

        _ = group
            .MapPost(
                "/programs",
                static async (
                    CreateLearningProgramCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateLearningProgramCommand,
                        LearningProgramResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/lms/programs/{res.Value.Id:D}", res.Value)
                        : LmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateLmsProgram");
    }

    private static void MapTerms(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/terms",
                static async (
                    CreateLearningTermCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateLearningTermCommand, LearningTermResponse>(
                        cmd,
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Created($"/api/lms/terms/{res.Value.Id:D}", res.Value)
                        : LmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateLmsTerm");
    }

    private static void MapCourses(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/courses",
                static async (
                    Guid? programId,
                    bool? publicOnly,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetLearningCoursesQuery,
                            IReadOnlyList<LearningCourseResponse>
                        >(new GetLearningCoursesQuery(programId, publicOnly == true), ct)
                    )
            )
            .WithName("GetLmsCourses");

        _ = group
            .MapPost(
                "/courses",
                static async (
                    CreateLearningCourseCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateLearningCourseCommand,
                        LearningCourseResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/lms/courses/{res.Value.Id:D}", res.Value)
                        : LmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateLmsCourse");

        _ = group
            .MapPost(
                "/courses/{courseId:guid}/requirements",
                static async (
                    Guid courseId,
                    AddCourseRequirementCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<AddCourseRequirementCommand, LearningCourseResponse>(
                            body with
                            {
                                LearningCourseId = courseId,
                            },
                            ct
                        )
                    )
            )
            .WithName("AddLmsCourseRequirement");
    }

    private static void MapClasses(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/classes",
                static async (Guid? courseId, IQueryDispatcher q, CancellationToken ct) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetLearningClassesQuery,
                            IReadOnlyList<LearningClassResponse>
                        >(new GetLearningClassesQuery(courseId), ct)
                    )
            )
            .WithName("GetLmsClasses");

        _ = group
            .MapPost(
                "/classes",
                static async (
                    CreateLearningClassCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateLearningClassCommand, LearningClassResponse>(
                        cmd,
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Created($"/api/lms/classes/{res.Value.Id:D}", res.Value)
                        : LmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateLmsClass");
    }

    private static void MapClassContentAndActivities(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/classes/{classId:guid}/content-pages",
                static async (
                    Guid classId,
                    AddClassContentPageCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            AddClassContentPageCommand,
                            LearningClassContentPageResponse
                        >(body with { LearningClassId = classId }, ct)
                    )
            )
            .WithName("AddLmsClassContentPage");

        _ = group
            .MapPost(
                "/classes/{classId:guid}/announcements",
                static async (
                    Guid classId,
                    AddClassAnnouncementCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            AddClassAnnouncementCommand,
                            LearningClassAnnouncementResponse
                        >(body with { LearningClassId = classId }, ct)
                    )
            )
            .WithName("AddLmsClassAnnouncement");

        _ = group
            .MapPost(
                "/classes/{classId:guid}/activities",
                static async (
                    Guid classId,
                    AddLearningActivityCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<AddLearningActivityCommand, LearningActivityResponse>(
                            body with
                            {
                                LearningClassId = classId,
                            },
                            ct
                        )
                    )
            )
            .WithName("AddLmsActivity");
    }

    private static void MapEnrollment(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/classes/{classId:guid}/enroll",
                static async (
                    Guid classId,
                    EnrollLearningParticipantCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            EnrollLearningParticipantCommand,
                            LearningParticipantResponse
                        >(body with { LearningClassId = classId }, ct)
                    )
            )
            .WithName("EnrollLmsParticipant");

        _ = group
            .MapPost(
                "/classes/{classId:guid}/activities/{activityId:guid}/submit",
                static async (
                    Guid classId,
                    Guid activityId,
                    SubmitLearningActivityCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            SubmitLearningActivityCommand,
                            LearningActivityCompletionResponse
                        >(
                            body with
                            {
                                LearningClassId = classId,
                                LearningActivityId = activityId,
                            },
                            ct
                        )
                    )
            )
            .WithName("SubmitLmsActivity");
    }

    private static void MapGradingAndPortal(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/grading-queue",
                static async (Guid? classId, IQueryDispatcher q, CancellationToken ct) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetFacilitatorGradingQueueQuery,
                            IReadOnlyList<LearningActivityCompletionResponse>
                        >(new GetFacilitatorGradingQueueQuery(classId), ct)
                    )
            )
            .WithName("GetLmsFacilitatorGradingQueue");

        _ = group
            .MapPost(
                "/completions/{completionId:guid}/grade",
                static async (
                    Guid completionId,
                    GradeLearningActivityCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            GradeLearningActivityCommand,
                            LearningActivityCompletionResponse
                        >(body with { CompletionId = completionId }, ct)
                    )
            )
            .WithName("GradeLmsActivity");

        _ = group
            .MapGet(
                "/portal/{personId:guid}",
                static async (Guid personId, IQueryDispatcher q, CancellationToken ct) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetLearnerPortalQuery,
                            IReadOnlyList<LearnerClassWorkspaceResponse>
                        >(new GetLearnerPortalQuery(personId), ct)
                    )
            )
            .WithName("GetLmsLearnerPortal");
    }

    private static void MapPublicCatalog(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapGet(
                "/api/lms/catalog",
                static async (
                    Guid? personId,
                    Guid? programId,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    LmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetPublicCourseCatalogQuery,
                            IReadOnlyList<PublicCourseCatalogItemResponse>
                        >(new GetPublicCourseCatalogQuery(personId, programId), ct)
                    )
            )
            .WithTags("LMS Public Catalog")
            .WithName("GetLmsPublicCourseCatalog")
            .AllowAnonymous();
    }
}
