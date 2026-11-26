using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Slices.Registrations;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Events.Slices;

internal static class EventsW4S8SliceRegistration
{
    public static void RegisterW4S8Services(IServiceCollection services)
    {
        services.TryAddScoped<RegistrationInventoryLoader>();
        services.TryAddScoped<RegistrationFinanceSettlementService>();
        services.TryAddScoped<RegistrationGroupPlacementCollaborator>();
        services.TryAddScoped<WaitlistPromotionService>();
        _ = services.AddScheduledJob<ExpiredRegistrationSessionCleanupJob>();

        RegisterTemplateAndInstanceHandlers(services);
        RegisterSubmissionAndWaitlistHandlers(services);
    }

    private static void RegisterTemplateAndInstanceHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateRegistrationTemplateCommand,
            RegistrationTemplateResponse,
            CreateRegistrationTemplateCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateRegistrationTemplateCommand,
            RegistrationTemplateResponse,
            UpdateRegistrationTemplateCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetRegistrationTemplatesQuery,
            IReadOnlyList<RegistrationTemplateResponse>,
            GetRegistrationTemplatesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetRegistrationTemplateByIdQuery,
            RegistrationTemplateResponse,
            GetRegistrationTemplateByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateRegistrationInstanceCommand,
            RegistrationInstanceResponse,
            CreateRegistrationInstanceCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateRegistrationInstanceCommand,
            RegistrationInstanceResponse,
            UpdateRegistrationInstanceCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetRegistrationInstancesQuery,
            IReadOnlyList<RegistrationInstanceResponse>,
            GetRegistrationInstancesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetRegistrationInstancePortalQuery,
            PublicRegistrationWizardInitResponse,
            GetRegistrationInstancePortalQueryHandler
        >();
    }

    private static void RegisterSubmissionAndWaitlistHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateRegistrationSessionHoldCommand,
            RegistrationSessionResponse,
            CreateRegistrationSessionHoldCommandHandler
        >();
        _ = services.AddCommandHandler<
            ExpireRegistrationSessionsCommand,
            RegistrationSessionCleanupResultResponse,
            ExpireRegistrationSessionsCommandHandler
        >();
        _ = services.AddQueryHandler<
            PreviewRegistrationPricingQuery,
            RegistrationPricingQuoteResponse,
            PreviewRegistrationPricingQueryHandler
        >();
        _ = services.AddCommandHandler<
            SubmitRegistrationCommand,
            RegistrationResponse,
            SubmitRegistrationCommandHandler
        >();
        _ = services.AddCommandHandler<
            RecordRegistrationPaymentCommand,
            RegistrationResponse,
            RecordRegistrationPaymentCommandHandler
        >();
        _ = services.AddCommandHandler<
            CancelRegistrantCommand,
            CancelRegistrantResultResponse,
            CancelRegistrantCommandHandler
        >();
        _ = services.AddCommandHandler<
            PlaceRegistrantInGroupCommand,
            RegistrationRegistrantResponse,
            PlaceRegistrantInGroupCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetRegistrationsByInstanceQuery,
            IReadOnlyList<RegistrationResponse>,
            GetRegistrationsByInstanceQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetRegistrationByConfirmationOrIdQuery,
            RegistrationResponse,
            GetRegistrationByConfirmationOrIdQueryHandler
        >();
    }
}
