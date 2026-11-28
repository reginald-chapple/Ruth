using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Host.Endpoints;
using Ruth.Modules.CheckIn;
using Ruth.Modules.Cms;
using Ruth.Modules.Commerce;
using Ruth.Modules.Communication;
using Ruth.Modules.Core;
using Ruth.Modules.Crm;
using Ruth.Modules.Engagement;
using Ruth.Modules.Events;
using Ruth.Modules.Finance;
using Ruth.Modules.Groups;
using Ruth.Modules.Identity;
using Ruth.Modules.Lms;
using Ruth.Modules.Reporting;
using Ruth.Modules.Search;
using Ruth.Modules.Tenancy;
using Ruth.Modules.Workflow;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.ArchitectureTests;

internal static class EndpointAuthorizationTestAppFactory
{
    public static WebApplication BuildUnstartedAppWithAllEndpoints()
    {
        var builder = WebApplication.CreateSlimBuilder();
        _ = builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ConnectionStrings:RuthDatabase"] =
                    "Host=127.0.0.1;Port=5432;Database=ruth_arch_inspect;Username=ruth;Password=ruth",
                ["ConnectionStrings:Redis"] = "127.0.0.1:6379,abortConnect=false",
            }
        );

        RegisterAllServices(builder.Services, builder.Configuration);
        var app = builder.Build();
        MapAllEndpoints(app);
        return app;
    }

    private static void RegisterAllServices(IServiceCollection services, IConfiguration cfg)
    {
        _ = services.AddRouting();
        _ = services.AddSignalR();
        _ = services.AddHealthChecks();
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(cfg);
        _ = services.AddIdentityModule(cfg);
        _ = services.AddCoreModule(cfg);
        _ = services.AddCrmModule(cfg);
        _ = services.AddGroupsModule(cfg);
        _ = services.AddCheckInModule(cfg);
        _ = services.AddCommunicationModule(cfg);
        _ = services.AddFinanceModule(cfg);
        _ = services.AddEventsModule(cfg);
        _ = services.AddCommerceModule(cfg);
        _ = services.AddEngagementModule(cfg);
        _ = services.AddWorkflowModule(cfg);
        _ = services.AddCmsModule(cfg);
        _ = services.AddReportingModule(cfg);
        _ = services.AddLmsModule(cfg);
        _ = services.AddSearchModule(cfg);
        _ = services.AddTenancyModule(cfg);
    }

    private static void MapAllEndpoints(WebApplication app)
    {
        _ = app.MapHealthEndpoints();
        _ = app.MapTenancyModuleEndpoints();
        _ = app.MapIdentityModuleEndpoints();
        _ = app.MapCoreModuleEndpoints();
        _ = app.MapCrmModuleEndpoints();
        _ = app.MapGroupsModuleEndpoints();
        _ = app.MapCheckInModuleEndpoints();
        _ = app.MapCommunicationModuleEndpoints();
        _ = app.MapFinanceModuleEndpoints();
        _ = app.MapEventsModuleEndpoints();
        _ = app.MapCommerceModuleEndpoints();
        _ = app.MapEngagementModuleEndpoints();
        _ = app.MapWorkflowModuleEndpoints();
        _ = app.MapCmsModuleEndpoints();
        _ = app.MapReportingModuleEndpoints();
        _ = app.MapLmsModuleEndpoints();
        _ = app.MapSearchModuleEndpoints();
    }
}
