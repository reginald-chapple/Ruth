using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.Modules.CheckIn.Infrastructure.Printing;
using Ruth.Modules.CheckIn.Infrastructure.WalletPasses;
using Ruth.Modules.CheckIn.Slices.Configurations;
using Ruth.Modules.CheckIn.Slices.Labels;
using Ruth.Modules.CheckIn.Slices.Sessions;
using Ruth.Modules.CheckIn.Slices.WalletPasses;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.CheckIn;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the CheckIn module.</summary>
public static class CheckInModuleExtensions
{
    /// <summary>Registers the CheckIn module DbContext, bootstrap migrations, entity catalog, and slice handlers.</summary>
    public static IServiceCollection AddCheckInModule(
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

        _ = services.AddModuleDbContext<CheckInDbContext>(
            connectionString,
            CheckInDbContext.SchemaName
        );

        _ = services.Configure<ZplPrintingOptions>(
            configuration.GetSection(ZplPrintingOptions.SectionName)
        );
        _ = services.Configure<WalletPassSecurityOptions>(
            configuration.GetSection(WalletPassSecurityOptions.SectionName)
        );
        _ = services.AddHostedService<CheckInBootstrapHostedService>();
        CheckInConfigurationSliceRegistration.RegisterHandlers(services);
        CheckInSessionSliceRegistration.RegisterHandlers(services);
        CheckInLabelSliceRegistration.RegisterHandlers(services);
        WalletPassSliceRegistration.RegisterHandlers(services);

        foreach (var entityType in CheckInEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps all Minimal API endpoints owned by the CheckIn module.</summary>
    public static IEndpointRouteBuilder MapCheckInModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapCheckInConfigurationEndpoints();
        _ = endpoints.MapCheckInSessionEndpoints();
        _ = endpoints.MapCheckInLabelEndpoints();
        _ = endpoints.MapWalletPassStaffAndClaimEndpoints();
        _ = endpoints.MapApplePassKitWebServiceEndpoints();
        return endpoints;
    }
}
