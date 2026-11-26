using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Contracts.Grading;
using Ruth.Modules.Lms.Contracts.Portal;
using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.Modules.Lms.Slices.Classes;
using Ruth.Modules.Lms.Slices.Courses;
using Ruth.Modules.Lms.Slices.Events;
using Ruth.Modules.Lms.Slices.Grading;
using Ruth.Modules.Lms.Slices.Portal;
using Ruth.Modules.Lms.Slices.Programs;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Slices;

internal static class LmsSlicesRegistration
{
    public static void Register(IServiceCollection services)
    {
        RegisterProgramsAndCourses(services);
        RegisterClassesAndPortal(services);
    }

    private static void RegisterProgramsAndCourses(IServiceCollection services)
    {
        _ = services.AddScoped<
            ICommandHandler<CreateLearningGradingSystemCommand, LearningGradingSystemResponse>,
            CreateLearningGradingSystemCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<
                GetLearningGradingSystemsQuery,
                IReadOnlyList<LearningGradingSystemResponse>
            >,
            GetLearningGradingSystemsQueryHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<CreateLearningProgramCommand, LearningProgramResponse>,
            CreateLearningProgramCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetLearningProgramsQuery, IReadOnlyList<LearningProgramResponse>>,
            GetLearningProgramsQueryHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<CreateLearningTermCommand, LearningTermResponse>,
            CreateLearningTermCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetLearningTermsQuery, IReadOnlyList<LearningTermResponse>>,
            GetLearningTermsQueryHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<CreateLearningCourseCommand, LearningCourseResponse>,
            CreateLearningCourseCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<AddCourseRequirementCommand, LearningCourseResponse>,
            AddCourseRequirementCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetLearningCoursesQuery, IReadOnlyList<LearningCourseResponse>>,
            GetLearningCoursesQueryHandler
        >();
    }

    private static void RegisterClassesAndPortal(IServiceCollection services)
    {
        _ = services.AddScoped<LmsProgressRecalculator>();
        _ = services.AddScoped<
            ICommandHandler<CreateLearningClassCommand, LearningClassResponse>,
            CreateLearningClassCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<AddClassContentPageCommand, LearningClassContentPageResponse>,
            AddClassContentPageCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<AddClassAnnouncementCommand, LearningClassAnnouncementResponse>,
            AddClassAnnouncementCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<AddLearningActivityCommand, LearningActivityResponse>,
            AddLearningActivityCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<EnrollLearningParticipantCommand, LearningParticipantResponse>,
            EnrollLearningParticipantCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<SubmitLearningActivityCommand, LearningActivityCompletionResponse>,
            SubmitLearningActivityCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<GradeLearningActivityCommand, LearningActivityCompletionResponse>,
            GradeLearningActivityCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetLearningClassesQuery, IReadOnlyList<LearningClassResponse>>,
            GetLearningClassesQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<
                GetFacilitatorGradingQueueQuery,
                IReadOnlyList<LearningActivityCompletionResponse>
            >,
            GetFacilitatorGradingQueueQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<
                GetPublicCourseCatalogQuery,
                IReadOnlyList<PublicCourseCatalogItemResponse>
            >,
            GetPublicCourseCatalogQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetLearnerPortalQuery, IReadOnlyList<LearnerClassWorkspaceResponse>>,
            GetLearnerPortalQueryHandler
        >();
        _ = services.AddScoped<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>,
            LmsPersonMergedConsumer
        >();
    }
}
