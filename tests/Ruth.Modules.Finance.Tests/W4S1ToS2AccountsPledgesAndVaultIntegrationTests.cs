using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Organizations;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

[Collection(FinancePostgresFixtures.Name)]
public sealed class W4S1ToS2AccountsPledgesAndVaultIntegrationTests(
    FinancePostgresContainerFixture postgres
)
{
    [Fact]
    public async Task Accounts_HierarchyCycleDetection_Organizations_AndPledgeAnalytics_WorkEndToEnd()
    {
        var db = await postgres.CreateIsolatedDatabaseAsync();
        await using var root = await FinanceTestHostFactory.BuildAndMigrateAsync(db);
        await using var scope = root.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var seeded = await queries.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(new GetFinancialAccountsQuery());
        Assert.True(seeded.IsSuccess);
        Assert.True(seeded.Value.Count >= 3);

        var parent = await commands.SendAsync<
            CreateFinancialAccountCommand,
            FinancialAccountResponse
        >(
            new CreateFinancialAccountCommand(
                new FinancialAccountSpec(
                    "Capital Endowment",
                    PublicName: null,
                    "ENDOW-MAIN",
                    FinancialAccountKind.CharitableFund,
                    IsTaxDeductible: true
                )
            )
        );
        Assert.True(parent.IsSuccess);

        var dupCode = await commands.SendAsync<
            CreateFinancialAccountCommand,
            FinancialAccountResponse
        >(
            new CreateFinancialAccountCommand(
                new FinancialAccountSpec(
                    "Duplicate Code",
                    PublicName: null,
                    "endow-main",
                    FinancialAccountKind.CharitableFund,
                    IsTaxDeductible: true
                )
            )
        );
        Assert.True(dupCode.IsFailure);

        var child = await commands.SendAsync<
            CreateFinancialAccountCommand,
            FinancialAccountResponse
        >(
            new CreateFinancialAccountCommand(
                new FinancialAccountSpec(
                    "STEM Scholarship Sub-Fund",
                    PublicName: null,
                    "ENDOW-STEM",
                    FinancialAccountKind.GrantFund,
                    IsTaxDeductible: true,
                    ParentAccountId: parent.Value.Id
                )
            )
        );
        Assert.True(child.IsSuccess);

        var cycleAttempt = await commands.SendAsync<
            UpdateFinancialAccountCommand,
            FinancialAccountResponse
        >(
            new UpdateFinancialAccountCommand(
                parent.Value.Id,
                new FinancialAccountSpec(
                    "Capital Endowment",
                    PublicName: null,
                    "ENDOW-MAIN",
                    FinancialAccountKind.CharitableFund,
                    IsTaxDeductible: true,
                    ParentAccountId: child.Value.Id
                )
            )
        );
        Assert.True(cycleAttempt.IsFailure);

        await VerifyOrganizationAndPledgeAnalyticsAsync(commands, queries, child.Value.Id);
    }

    private static async Task VerifyOrganizationAndPledgeAnalyticsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid accountId
    )
    {
        var org = await commands.SendAsync<CreateOrganizationCommand, OrganizationDetailResponse>(
            new CreateOrganizationCommand(
                "Horizon Foundation",
                Email: "grants@horizon.test",
                PhoneNumber: "303-555-0199",
                TaxIdentifier: "84-1234567",
                SponsorTier: "Platinum",
                WebsiteUrl: "https://horizon.test"
            )
        );
        Assert.True(org.IsSuccess);
        var orgPersonId = org.Value.OrganizationPersonId;

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var pledge = await commands.SendAsync<
            CreateFinancialPledgeCommand,
            FinancialPledgeResponse
        >(
            new CreateFinancialPledgeCommand(
                orgPersonId,
                accountId,
                10000.00m,
                "USD",
                PledgeFrequency.Quarterly,
                today.AddDays(-10),
                today.AddDays(80),
                Notes: "STEM Grant Commitment"
            )
        );
        Assert.True(pledge.IsSuccess);

        var payment = await commands.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                orgPersonId,
                "USD",
                FinancialTransactionSourceType.StaffEntry,
                FinancialTenderType.Wire,
                [new FinancialTransactionSplitSpec(accountId, 4000.00m, Summary: "Installment 1")]
            )
        );
        Assert.True(payment.IsSuccess);

        var analytics = await queries.QueryAsync<
            GetPledgeAnalyticsQuery,
            PledgeAnalyticsSummaryResponse
        >(new GetPledgeAnalyticsQuery(AccountId: accountId, AsOfDate: today));
        Assert.True(analytics.IsSuccess);
        Assert.Equal(10000.00m, analytics.Value.TotalPledgedAmount);
        Assert.Equal(4000.00m, analytics.Value.TotalFulfilledAmount);

        var orgDetail = await queries.QueryAsync<
            GetOrganizationByIdQuery,
            OrganizationDetailResponse
        >(new GetOrganizationByIdQuery(orgPersonId));
        Assert.True(orgDetail.IsSuccess);
        Assert.Equal(4000.00m, orgDetail.Value.LifetimeSettledTotal);
    }
}
