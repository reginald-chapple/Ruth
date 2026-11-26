using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Slices.Pipelines;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Engagement.Slices;

internal static class W5S1PipelineRegistration
{
    public static void Register(IServiceCollection services)
    {
        services.TryAddScoped<PipelineAutomationCollaborator>();
        _ = services.AddScheduledJob<DueEngagementFollowUpWakeupJob>();

        _ = services.AddCommandHandler<
            CreatePipelineTypeCommand,
            PipelineTypeResponse,
            CreatePipelineTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdatePipelineTypeCommand,
            PipelineTypeResponse,
            UpdatePipelineTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPipelineTypesQuery,
            IReadOnlyList<PipelineTypeResponse>,
            GetPipelineTypesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetPipelineTypeByIdOrSlugQuery,
            PipelineTypeResponse,
            GetPipelineTypeByIdOrSlugQueryHandler
        >();

        _ = services.AddCommandHandler<
            CreateEngagementOpportunityCommand,
            EngagementOpportunityResponse,
            CreateEngagementOpportunityCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateEngagementOpportunityCommand,
            EngagementOpportunityResponse,
            UpdateEngagementOpportunityCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetEngagementOpportunitiesQuery,
            IReadOnlyList<EngagementOpportunityResponse>,
            GetEngagementOpportunitiesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetEngagementOpportunityByIdOrSlugQuery,
            EngagementOpportunityResponse,
            GetEngagementOpportunityByIdOrSlugQueryHandler
        >();

        RegisterRequestLifecycleHandlers(services);
        RegisterBoardAndQueryHandlers(services);
    }

    private static void RegisterRequestLifecycleHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateEngagementRequestCommand,
            EngagementRequestResponse,
            CreateEngagementRequestCommandHandler
        >();
        _ = services.AddCommandHandler<
            SubmitPublicEngagementSignupCommand,
            EngagementRequestResponse,
            SubmitPublicEngagementSignupCommandHandler
        >();
        _ = services.AddCommandHandler<
            AssignEngagementConnectorCommand,
            EngagementRequestResponse,
            AssignEngagementConnectorCommandHandler
        >();
        _ = services.AddCommandHandler<
            TransitionEngagementRequestStageCommand,
            EngagementRequestResponse,
            TransitionEngagementRequestStageCommandHandler
        >();
        _ = services.AddCommandHandler<
            RecordEngagementActivityCommand,
            EngagementRequestResponse,
            RecordEngagementActivityCommandHandler
        >();
        _ = services.AddCommandHandler<
            ScheduleEngagementFollowUpCommand,
            EngagementRequestResponse,
            ScheduleEngagementFollowUpCommandHandler
        >();
        _ = services.AddCommandHandler<
            ConnectEngagementRequestCommand,
            EngagementRequestResponse,
            ConnectEngagementRequestCommandHandler
        >();
        _ = services.AddCommandHandler<
            MarkEngagementRequestInactiveCommand,
            EngagementRequestResponse,
            MarkEngagementRequestInactiveCommandHandler
        >();
        _ = services.AddCommandHandler<
            TransferEngagementRequestCommand,
            EngagementRequestResponse,
            TransferEngagementRequestCommandHandler
        >();
    }

    private static void RegisterBoardAndQueryHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            WakeDueEngagementFollowUpsCommand,
            WakeupFollowUpsResponse,
            WakeDueEngagementFollowUpsCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetConnectorBoardQuery,
            ConnectorBoardResponse,
            GetConnectorBoardQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetEngagementRequestsQuery,
            IReadOnlyList<EngagementRequestResponse>,
            GetEngagementRequestsQueryHandler
        >();
    }
}
