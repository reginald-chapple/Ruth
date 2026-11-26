using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.Modules.Reporting.Slices;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.Reporting;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the Reporting module.</summary>
public static class ReportingModuleExtensions
{
    public static IServiceCollection AddReportingModule(
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

        _ = services.AddModuleDbContext<ReportingDbContext>(
            connectionString,
            ReportingDbContext.SchemaName
        );
        _ = services.AddHostedService<ReportingBootstrapHostedService>();

        ReportingSlicesRegistration.Register(services);

        foreach (var entityType in ReportingEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapReportingModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapReportingDataViewsAndReportsEndpoints();
        _ = endpoints.MapReportingMetricsAndAnalyticsEndpoints();
        return endpoints;
    }
}
