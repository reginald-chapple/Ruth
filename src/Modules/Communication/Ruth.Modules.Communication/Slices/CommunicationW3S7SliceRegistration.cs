using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Slices.Flows;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Communication.Slices;

internal static class CommunicationW3S7SliceRegistration
{
    public static void RegisterW3S7Services(IServiceCollection services)
    {
        services.TryAddScoped<FlowStepSenderCollaborator>();
        services.TryAddScoped<FlowAttendanceConversionEvaluator>();

        _ = services.AddCommandHandler<
            CreateCommunicationFlowCommand,
            CommunicationFlowResponse,
            CreateCommunicationFlowCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCommunicationFlowCommand,
            CommunicationFlowResponse,
            UpdateCommunicationFlowCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteCommunicationFlowCommand,
            bool,
            DeleteCommunicationFlowCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCommunicationFlowsQuery,
            IReadOnlyList<CommunicationFlowResponse>,
            GetCommunicationFlowsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCommunicationFlowByIdQuery,
            CommunicationFlowResponse,
            GetCommunicationFlowByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            EnrollCommunicationFlowRecipientsCommand,
            CommunicationFlowInstanceResponse,
            EnrollCommunicationFlowRecipientsCommandHandler
        >();
        _ = services.AddCommandHandler<
            RecordFlowConversionCommand,
            CommunicationFlowConversionResponse,
            RecordFlowConversionCommandHandler
        >();
        _ = services.AddCommandHandler<
            ProcessDueCommunicationFlowsCommand,
            FlowAdvancementBatchResultResponse,
            ProcessDueCommunicationFlowsCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCommunicationFlowInstancesQuery,
            IReadOnlyList<CommunicationFlowInstanceResponse>,
            GetCommunicationFlowInstancesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCommunicationFlowAnalyticsQuery,
            CommunicationFlowAnalyticsResponse,
            GetCommunicationFlowAnalyticsQueryHandler
        >();

        _ = services.AddScheduledJob<CommunicationFlowAdvancementJob>();
    }
}
