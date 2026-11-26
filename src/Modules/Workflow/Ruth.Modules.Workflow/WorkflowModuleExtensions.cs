using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.Modules.Workflow.Slices;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.Workflow;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the Workflow module.</summary>
public static class WorkflowModuleExtensions
{
    /// <summary>Registers the Workflow module DbContext, bootstrap migrations, entity catalog, scheduled jobs, and W5-S5/W5-S6 handlers.</summary>
    public static IServiceCollection AddWorkflowModule(
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

        _ = services.AddModuleDbContext<WorkflowDbContext>(
            connectionString,
            WorkflowDbContext.SchemaName
        );
        _ = services.AddHostedService<WorkflowBootstrapHostedService>();

        W5S5WorkflowEngineRegistration.Register(services);
        W5S6WorkflowFormRegistration.Register(services);

        foreach (var entityType in WorkflowEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps all Minimal API endpoints owned by the Workflow module.</summary>
    public static IEndpointRouteBuilder MapWorkflowModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapWorkflowEngineEndpoints();
        _ = endpoints.MapWorkflowTriggerEndpoints();
        _ = endpoints.MapWorkflowFormEndpoints();
        return endpoints;
    }
}
