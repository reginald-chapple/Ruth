using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Infrastructure.Templating;
using Ruth.Modules.Communication.Slices;
using Ruth.Modules.Communication.Slices.BulkCommunications;
using Ruth.Modules.Communication.Slices.Flows;
using Ruth.Modules.Communication.Slices.PersonalDevices;
using Ruth.Modules.Communication.Slices.PhoneNumbers;
using Ruth.Modules.Communication.Slices.SystemCommunications;
using Ruth.Modules.Communication.Slices.Templates;
using Ruth.Modules.Communication.Slices.Transports;
using Ruth.Modules.Communication.Slices.TwoWaySms;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Communication;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the Communication module.</summary>
public static class CommunicationModuleExtensions
{
    /// <summary>Registers the Communication module DbContext, Razor template renderer, safe merge engine, transports, and slice handlers.</summary>
    public static IServiceCollection AddCommunicationModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "RuthDatabase"
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' was not found."
            );

        _ = services.AddModuleDbContext<CommunicationDbContext>(
            connectionString,
            CommunicationDbContext.SchemaName
        );

        services.TryAddScoped<CommunicationTenantSeedContributor>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<ITenantSeedContributor, CommunicationTenantSeedContributor>()
        );
        _ = services.AddHostedService<CommunicationBootstrapHostedService>();

        _ = services.AddScoped<ITemplateSnippetStore, DbTemplateSnippetStore>();
        _ = services.AddScoped<IMergeTemplateEngine, SafeMergeTemplateEngine>();

        foreach (var descriptor in BuiltInRazorTemplatesCatalog.GetAll())
        {
            _ = services.AddSingleton(descriptor);
        }

        _ = services.AddSingleton<IRazorTemplateRegistry, RazorTemplateRegistry>();
        _ = services.AddSingleton<IRazorTemplateRenderer, RazorHtmlTemplateRenderer>();
        services.TryAddSingleton<CommunicationInboundWebhookGuard>();

        RegisterTemplateHandlers(services);
        CommunicationW3S5SliceRegistration.RegisterW3S5Services(services, configuration);
        CommunicationW3S6SliceRegistration.RegisterW3S6Services(services, configuration);
        CommunicationW3S7SliceRegistration.RegisterW3S7Services(services);

        foreach (var entityType in CommunicationEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Registers an additional compile-time Razor component template with the global template registry.</summary>
    public static IServiceCollection AddRazorTemplate(
        this IServiceCollection services,
        RazorTemplateDescriptor descriptor
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(descriptor);
        _ = services.AddSingleton(descriptor);
        return services;
    }

    /// <summary>Maps all Minimal API endpoints owned by the Communication module.</summary>
    public static IEndpointRouteBuilder MapCommunicationModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapTemplateEndpoints();
        _ = endpoints.MapCommunicationTransportEndpoints();
        _ = endpoints.MapSystemCommunicationEndpoints();
        _ = endpoints.MapSystemPhoneNumberEndpoints();
        _ = endpoints.MapPersonalDeviceEndpoints();
        _ = endpoints.MapBulkCommunicationTemplateEndpoints();
        _ = endpoints.MapBulkCommunicationEndpoints();
        _ = endpoints.MapCommunicationWebhookAndPreferenceEndpoints();
        _ = endpoints.MapTwoWaySmsEndpoints();
        _ = endpoints.MapCommunicationFlowEndpoints();
        return endpoints;
    }

    private static void RegisterTemplateHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateTemplateSnippetCommand,
            TemplateSnippetResponse,
            CreateTemplateSnippetCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateTemplateSnippetCommand,
            TemplateSnippetResponse,
            UpdateTemplateSnippetCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteTemplateSnippetCommand,
            bool,
            DeleteTemplateSnippetCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetTemplateSnippetsQuery,
            IReadOnlyList<TemplateSnippetResponse>,
            GetTemplateSnippetsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetTemplateSnippetByIdQuery,
            TemplateSnippetResponse,
            GetTemplateSnippetByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetRegisteredRazorTemplatesQuery,
            IReadOnlyList<RazorTemplateSummaryResponse>,
            GetRegisteredRazorTemplatesQueryHandler
        >();
        _ = services.AddCommandHandler<
            PreviewMergeTemplateCommand,
            TemplatePreviewResponse,
            PreviewMergeTemplateCommandHandler
        >();
        _ = services.AddCommandHandler<
            PreviewRazorTemplateCommand,
            TemplatePreviewResponse,
            PreviewRazorTemplateCommandHandler
        >();
    }
}
