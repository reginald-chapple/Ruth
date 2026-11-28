using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Finance.Contracts.Events;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.Modules.Workflow.Slices.Actions;
using Ruth.Modules.Workflow.Slices.Definitions;
using Ruth.Modules.Workflow.Slices.Engine;
using Ruth.Modules.Workflow.Slices.Events;
using Ruth.Modules.Workflow.Slices.Instances;
using Ruth.Modules.Workflow.Slices.Triggers;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Workflow.Slices;

internal static class W5S5WorkflowEngineRegistration
{
    public static void Register(IServiceCollection services)
    {
        _ = services
            .AddHttpClient(WebhookPostWorkflowAction.HttpClientName, static client =>
                client.Timeout = TimeSpan.FromSeconds(15)
            )
            .ConfigurePrimaryHttpMessageHandler(static () =>
                SafeOutboundHttpHandler.CreateSocketsHandler()
            )
            .AddHttpMessageHandler(static () => SafeOutboundHttpHandler.CreatePipelineHandler());

        services.TryAddScoped<WorkflowExecutionEngine>();
        RegisterCoreActionPlugins(services);
        RegisterDefinitionHandlers(services);
        RegisterInstanceHandlers(services);
        RegisterTriggerAndConsumers(services);
    }

    private static void RegisterCoreActionPlugins(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, SetWorkflowStatusControlAction>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, ConditionalActivateActivityAction>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, SetAttributeValueAction>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, RenderMergeOrRazorTemplateAction>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, UpsertCrmPersonWorkflowAction>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, AddGroupMemberWorkflowAction>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, CreateFinanceInvoiceWorkflowAction>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, CreateEngagementRequestWorkflowAction>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, SendSystemNotificationWorkflowAction>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, WebhookPostWorkflowAction>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IWorkflowAction, UserEntryFormWorkflowAction>()
        );
    }

    private static void RegisterDefinitionHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetWorkflowActionCatalogQuery,
            IReadOnlyList<WorkflowActionPluginDescriptor>,
            GetWorkflowActionCatalogQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateWorkflowTypeCommand,
            WorkflowTypeResponse,
            CreateWorkflowTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateWorkflowTypeCommand,
            WorkflowTypeResponse,
            UpdateWorkflowTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetWorkflowTypesQuery,
            IReadOnlyList<WorkflowTypeResponse>,
            GetWorkflowTypesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetWorkflowTypeByIdOrSlugQuery,
            WorkflowTypeResponse,
            GetWorkflowTypeByIdOrSlugQueryHandler
        >();
    }

    private static void RegisterInstanceHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            LaunchWorkflowCommand,
            WorkflowInstanceResponse,
            LaunchWorkflowCommandHandler
        >();
        _ = services.AddCommandHandler<
            ProcessWorkflowCommand,
            WorkflowInstanceResponse,
            ProcessWorkflowCommandHandler
        >();
        _ = services.AddCommandHandler<
            ProcessPendingWorkflowsCommand,
            WorkflowBatchProcessResponse,
            ProcessPendingWorkflowsCommandHandler
        >();
        _ = services.AddCommandHandler<
            CancelWorkflowCommand,
            WorkflowInstanceResponse,
            CancelWorkflowCommandHandler
        >();
        _ = services.AddCommandHandler<
            AssignWorkflowActivityCommand,
            WorkflowInstanceResponse,
            AssignWorkflowActivityCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetWorkflowInstancesQuery,
            IReadOnlyList<WorkflowInstanceResponse>,
            GetWorkflowInstancesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetWorkflowInstanceByIdQuery,
            WorkflowInstanceResponse,
            GetWorkflowInstanceByIdQueryHandler
        >();
        _ = services.AddScheduledJob<WorkflowBackgroundProcessingJob>();
    }

    private static void RegisterTriggerAndConsumers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateWorkflowTriggerCommand,
            WorkflowTriggerResponse,
            CreateWorkflowTriggerCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateWorkflowTriggerCommand,
            WorkflowTriggerResponse,
            UpdateWorkflowTriggerCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetWorkflowTriggersQuery,
            IReadOnlyList<WorkflowTriggerResponse>,
            GetWorkflowTriggersQueryHandler
        >();
        _ = services.AddCommandHandler<
            DispatchWorkflowAutomationEventCommand,
            WorkflowAutomationDispatchResponse,
            DispatchWorkflowAutomationEventCommandHandler
        >();

        _ = services.AddIdempotentIntegrationEventConsumer<
            EngagementRequestConnectedIntegrationEvent,
            EngagementConnectedWorkflowTriggerConsumer,
            WorkflowDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            SupportRequestSubmittedIntegrationEvent,
            SupportRequestWorkflowTriggerConsumer,
            WorkflowDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            ServiceBookingConfirmedIntegrationEvent,
            BookingConfirmedWorkflowTriggerConsumer,
            WorkflowDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            InvoiceSettledIntegrationEvent,
            InvoiceSettledWorkflowTriggerConsumer,
            WorkflowDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            WorkflowPersonMergedConsumer,
            WorkflowDbContext
        >();
    }
}
