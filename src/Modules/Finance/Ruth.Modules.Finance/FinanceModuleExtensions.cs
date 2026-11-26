using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Slices;
using Ruth.Modules.Finance.Slices.Accounts;
using Ruth.Modules.Finance.Slices.Assistance;
using Ruth.Modules.Finance.Slices.Batches;
using Ruth.Modules.Finance.Slices.CadenceAlerts;
using Ruth.Modules.Finance.Slices.Campaigns;
using Ruth.Modules.Finance.Slices.Gateways;
using Ruth.Modules.Finance.Slices.Invoices;
using Ruth.Modules.Finance.Slices.Organizations;
using Ruth.Modules.Finance.Slices.PaymentMethods;
using Ruth.Modules.Finance.Slices.Pledges;
using Ruth.Modules.Finance.Slices.Recurring;
using Ruth.Modules.Finance.Slices.Statements;
using Ruth.Modules.Finance.Slices.Transactions;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Modules.Finance;

/// <summary>Service registration and Minimal API endpoint mapping extensions for the Finance module.</summary>
public static class FinanceModuleExtensions
{
    /// <summary>Registers the Finance module DbContext, bootstrap migrations, entity catalog, scheduled jobs, and slice handlers.</summary>
    public static IServiceCollection AddFinanceModule(
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

        _ = services.AddModuleDbContext<FinanceDbContext>(
            connectionString,
            FinanceDbContext.SchemaName
        );

        _ = services.AddHostedService<FinanceBootstrapHostedService>();
        FinanceW4S1SliceRegistration.RegisterW4S1Services(services);
        FinanceW4S2SliceRegistration.RegisterW4S2Services(services, configuration);
        FinanceW4S3SliceRegistration.RegisterW4S3Services(services);
        FinanceW4S4SliceRegistration.RegisterW4S4Services(services);
        FinanceW4S5SliceRegistration.RegisterW4S5Services(services);
        FinanceW4S6SliceRegistration.RegisterW4S6Services(services);

        foreach (var entityType in FinanceEntityTypeCatalog.GetAll())
        {
            _ = services.AddSingleton(entityType);
        }

        return services;
    }

    /// <summary>Maps all Minimal API endpoints owned by the Finance module.</summary>
    public static IEndpointRouteBuilder MapFinanceModuleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapFinancialAccountEndpoints();
        _ = endpoints.MapOrganizationEndpoints();
        _ = endpoints.MapFinancialPledgeEndpoints();
        _ = endpoints.MapFinancialGatewayEndpoints();
        _ = endpoints.MapSavedPaymentMethodEndpoints();
        _ = endpoints.MapInvoiceEndpoints();
        _ = endpoints.MapFinancialBatchEndpoints();
        _ = endpoints.MapFinancialTransactionEndpoints();
        _ = endpoints.MapRecurringScheduleEndpoints();
        _ = endpoints.MapCadenceAlertEndpoints();
        _ = endpoints.MapFinancialStatementEndpoints();
        _ = endpoints.MapAssistanceEndpoints();
        _ = endpoints.MapCampaignEndpoints();
        return endpoints;
    }
}
