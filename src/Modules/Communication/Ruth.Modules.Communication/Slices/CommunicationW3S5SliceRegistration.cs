using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Communication.Contracts.PersonalDevices;
using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.Modules.Communication.Slices.Events;
using Ruth.Modules.Communication.Slices.PersonalDevices;
using Ruth.Modules.Communication.Slices.PhoneNumbers;
using Ruth.Modules.Communication.Slices.SystemCommunications;
using Ruth.Modules.Communication.Slices.Transports;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Groups.Contracts.Events;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Communication.Slices;

internal static class CommunicationW3S5SliceRegistration
{
    public static void RegisterW3S5Services(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        _ = services.Configure<CommunicationTransportOptions>(
            configuration.GetSection(CommunicationTransportOptions.SectionName)
        );

        services.TryAddSingleton<InMemoryCommunicationSink>();
        _ = services.AddSingleton<ICommunicationTransport, DevInMemoryEmailTransport>();
        _ = services.AddSingleton<ICommunicationTransport, DevInMemorySmsTransport>();
        _ = services.AddSingleton<ICommunicationTransport, PushNotificationStubTransport>();
        services.TryAddScoped<CommunicationTransportResolver>();
        services.TryAddScoped<SystemCommunicationRecipientResolver>();
        services.TryAddScoped<SystemCommunicationRenderService>();

        RegisterTransportAndPhoneHandlers(services);
        RegisterDeviceAndNotificationHandlers(services);
        RegisterIntegrationEventConsumers(services);
    }

    private static void RegisterTransportAndPhoneHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetCommunicationTransportsQuery,
            IReadOnlyList<CommunicationTransportStatusResponse>,
            GetCommunicationTransportsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCommunicationTransportConfigsQuery,
            IReadOnlyList<CommunicationTransportConfigResponse>,
            GetCommunicationTransportConfigsQueryHandler
        >();
        _ = services.AddCommandHandler<
            UpsertCommunicationTransportConfigCommand,
            CommunicationTransportConfigResponse,
            UpsertCommunicationTransportConfigCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetInMemorySentMessagesQuery,
            IReadOnlyList<InMemoryCapturedMessageResponse>,
            GetInMemorySentMessagesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateSystemPhoneNumberCommand,
            SystemPhoneNumberResponse,
            CreateSystemPhoneNumberCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateSystemPhoneNumberCommand,
            SystemPhoneNumberResponse,
            UpdateSystemPhoneNumberCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteSystemPhoneNumberCommand,
            bool,
            DeleteSystemPhoneNumberCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSystemPhoneNumbersQuery,
            IReadOnlyList<SystemPhoneNumberResponse>,
            GetSystemPhoneNumbersQueryHandler
        >();
    }

    private static void RegisterDeviceAndNotificationHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            RegisterPersonalDeviceCommand,
            PersonalDeviceResponse,
            RegisterPersonalDeviceCommandHandler
        >();
        _ = services.AddCommandHandler<
            SetPersonalDeviceNotificationsCommand,
            PersonalDeviceResponse,
            SetPersonalDeviceNotificationsCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeletePersonalDeviceCommand,
            bool,
            DeletePersonalDeviceCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonalDevicesQuery,
            IReadOnlyList<PersonalDeviceResponse>,
            GetPersonalDevicesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateSystemCommunicationCommand,
            SystemCommunicationResponse,
            CreateSystemCommunicationCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateSystemCommunicationCommand,
            SystemCommunicationResponse,
            UpdateSystemCommunicationCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteSystemCommunicationCommand,
            bool,
            DeleteSystemCommunicationCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSystemCommunicationsQuery,
            IReadOnlyList<SystemCommunicationResponse>,
            GetSystemCommunicationsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetSystemCommunicationByIdQuery,
            SystemCommunicationResponse,
            GetSystemCommunicationByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            SendSystemCommunicationCommand,
            SystemCommunicationSendResultResponse,
            SendSystemCommunicationCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSystemNotificationLogsQuery,
            IReadOnlyList<SystemNotificationLogResponse>,
            GetSystemNotificationLogsQueryHandler
        >();
    }

    private static void RegisterIntegrationEventConsumers(IServiceCollection services)
    {
        _ = services.AddIdempotentIntegrationEventConsumer<
            OrganizationOnboardedIntegrationEvent,
            OrganizationOnboardedNotificationConsumer,
            CommunicationDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PasswordResetRequestedIntegrationEvent,
            PasswordResetNotificationConsumer,
            CommunicationDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            AccountConfirmationRequestedIntegrationEvent,
            AccountConfirmationNotificationConsumer,
            CommunicationDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonAttendedIntegrationEvent,
            GroupAttendanceNotificationConsumer,
            CommunicationDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            SystemNotificationRequestedIntegrationEvent,
            SystemNotificationRequestedConsumer,
            CommunicationDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            CommunicationPersonMergedConsumer,
            CommunicationDbContext
        >();
    }
}
