using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Organizations;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Slices.Accounts;
using Ruth.Modules.Finance.Slices.Organizations;
using Ruth.Modules.Finance.Slices.Pledges;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Finance.Slices;

internal static class FinanceW4S1SliceRegistration
{
    public static void RegisterW4S1Services(IServiceCollection services)
    {
        services.TryAddScoped<IFinanceLedgerSummaryReader, EmptyFinanceLedgerSummaryReader>();

        RegisterAccountHandlers(services);
        RegisterOrganizationHandlers(services);
        RegisterPledgeHandlers(services);
    }

    private static void RegisterAccountHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateFinancialAccountCommand,
            FinancialAccountResponse,
            CreateFinancialAccountCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateFinancialAccountCommand,
            FinancialAccountResponse,
            UpdateFinancialAccountCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>,
            GetFinancialAccountsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetFinancialAccountByIdQuery,
            FinancialAccountResponse,
            GetFinancialAccountByIdQueryHandler
        >();
    }

    private static void RegisterOrganizationHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateOrganizationCommand,
            OrganizationDetailResponse,
            CreateOrganizationCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateOrganizationCommand,
            OrganizationDetailResponse,
            UpdateOrganizationCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetOrganizationsQuery,
            IReadOnlyList<OrganizationSummaryResponse>,
            GetOrganizationsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetOrganizationByIdQuery,
            OrganizationDetailResponse,
            GetOrganizationByIdQueryHandler
        >();
    }

    private static void RegisterPledgeHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateFinancialPledgeCommand,
            FinancialPledgeResponse,
            CreateFinancialPledgeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateFinancialPledgeCommand,
            FinancialPledgeResponse,
            UpdateFinancialPledgeCommandHandler
        >();
        _ = services.AddCommandHandler<
            CancelFinancialPledgeCommand,
            FinancialPledgeResponse,
            CancelFinancialPledgeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetFinancialPledgesQuery,
            IReadOnlyList<FinancialPledgeResponse>,
            GetFinancialPledgesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetPledgeAnalyticsQuery,
            PledgeAnalyticsSummaryResponse,
            GetPledgeAnalyticsQueryHandler
        >();
    }
}
