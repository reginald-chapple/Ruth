using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Slices.CadenceAlerts;
using Ruth.Modules.Finance.Slices.Events;
using Ruth.Modules.Finance.Slices.Recurring;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Finance.Slices;

internal static class FinanceW4S4SliceRegistration
{
    public static void RegisterW4S4Services(IServiceCollection services)
    {
        services.TryAddScoped<RecurringGatewaySyncHelper>();
        services.TryAddScoped<GatewayWebhookSettlementCollaborator>();
        RegisterRecurringHandlers(services);
        RegisterCadenceAlertHandlers(services);

        _ = services.AddScheduledJob<RecurringSettlementSyncJob>();
        _ = services.AddScheduledJob<TransactionCadenceScannerJob>();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            FinancePersonMergedConsumer,
            FinanceDbContext
        >();
    }

    private static void RegisterRecurringHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateRecurringScheduleCommand,
            RecurringScheduleResponse,
            CreateRecurringScheduleCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateRecurringScheduleCommand,
            RecurringScheduleResponse,
            UpdateRecurringScheduleCommandHandler
        >();
        _ = services.AddCommandHandler<
            PauseRecurringScheduleCommand,
            RecurringScheduleResponse,
            PauseRecurringScheduleCommandHandler
        >();
        _ = services.AddCommandHandler<
            ResumeRecurringScheduleCommand,
            RecurringScheduleResponse,
            ResumeRecurringScheduleCommandHandler
        >();
        _ = services.AddCommandHandler<
            CancelRecurringScheduleCommand,
            RecurringScheduleResponse,
            CancelRecurringScheduleCommandHandler
        >();
        _ = services.AddCommandHandler<
            ProcessDueRecurringSchedulesCommand,
            RecurringSettlementRunResponse,
            ProcessDueRecurringSchedulesCommandHandler
        >();
        _ = services.AddCommandHandler<
            ProcessGatewayWebhookCommand,
            GatewayWebhookProcessResponse,
            ProcessGatewayWebhookCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetRecurringSchedulesQuery,
            IReadOnlyList<RecurringScheduleResponse>,
            GetRecurringSchedulesQueryHandler
        >();
    }

    private static void RegisterCadenceAlertHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateCadenceAlertTypeCommand,
            TransactionCadenceAlertTypeResponse,
            CreateCadenceAlertTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCadenceAlertTypeCommand,
            TransactionCadenceAlertTypeResponse,
            UpdateCadenceAlertTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCadenceAlertTypesQuery,
            IReadOnlyList<TransactionCadenceAlertTypeResponse>,
            GetCadenceAlertTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            RunTransactionCadenceScanCommand,
            CadenceScanSummaryResponse,
            RunTransactionCadenceScanCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCadenceAlertStatusCommand,
            TransactionCadenceAlertResponse,
            UpdateCadenceAlertStatusCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetTransactionCadenceAlertsQuery,
            IReadOnlyList<TransactionCadenceAlertResponse>,
            GetTransactionCadenceAlertsQueryHandler
        >();
    }
}
