using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

[Collection(FinancePostgresFixtures.Name)]
public sealed class W7S6FinanceMultiTenantRlsAndLiveGateIntegrationTests(
    FinancePostgresContainerFixture postgres
)
{
    [Fact]
    public async Task ForceRls_RuntimeRole_LivePaymentGate_CheckImageTenantPrefix_AndCrossTenantIsolation_WorkEndToEnd()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await FinanceTestHostFactory.BuildAndMigrateAsync(dbConn);

        await W7S6FinanceRlsTestHelper.AssertAllFinanceTablesHaveForceRlsAndFailClosedAsync(
            dbConn,
            provider
        );

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var (accountA, testGwA, liveGwA, testVaultMethodA) = await SeedTenantAInSandboxAsync(
            provider,
            tenantA
        );
        await VerifySandboxAndPendingLivePaymentGatesBlockLiveChargesAsync(
            provider,
            tenantA,
            accountA,
            testGwA,
            liveGwA,
            testVaultMethodA
        );
        await W7S6FinanceRlsTestHelper.VerifyLivePromotionAllowsLiveCaptureAndPrefixesStorageKeysAsync(
            provider,
            tenantA,
            accountA,
            liveGwA
        );
        await W7S6FinanceRlsTestHelper.VerifyTenantBIsolationAndCollidingAccountCodesAsync(
            provider,
            tenantB,
            accountA
        );
    }

    private static async Task<(
        Guid AccountId,
        Guid TestGatewayId,
        Guid LiveGatewayId,
        Guid TestVaultMethodId
    )> SeedTenantAInSandboxAsync(ServiceProvider provider, TenantId tenantA)
    {
        using (AmbientTenantContext.BeginScope(tenantA, "alpha-aid", TenantLifecycleStage.Sandbox))
        {
            await using var scope = provider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<FinanceTenantSeedContributor>();
            await seeder.SeedTenantAsync(tenantA);

            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var accounts = (
                await queries.QueryAsync<
                    GetFinancialAccountsQuery,
                    IReadOnlyList<FinancialAccountResponse>
                >(new GetFinancialAccountsQuery(ActiveOnly: true, IsTaxDeductible: true))
            ).Value;
            var accountId = accounts[0].Id;

            var gateways = (
                await queries.QueryAsync<
                    GetFinancialGatewaysQuery,
                    IReadOnlyList<FinancialGatewayResponse>
                >(new GetFinancialGatewaysQuery(ActiveOnly: true))
            ).Value;
            var testGwId = gateways[0].Id;

            var liveGw = (
                await commands.SendAsync<CreateFinancialGatewayCommand, FinancialGatewayResponse>(
                    new CreateFinancialGatewayCommand(
                        new FinancialGatewaySpec(
                            Name: "Production Live Gateway",
                            ProviderKey: TestPaymentGatewayProvider.CanonicalProviderKey,
                            SupportsCards: true,
                            SupportsAch: true,
                            SupportsRecurring: true,
                            IsTestMode: false,
                            IsActive: true
                        )
                    )
                )
            ).Value;

            var testMethod = (
                await commands.SendAsync<
                    VaultSavedPaymentMethodCommand,
                    SavedPaymentMethodResponse
                >(
                    new VaultSavedPaymentMethodCommand(
                        Guid.CreateVersion7(),
                        testGwId,
                        "tok_sandbox_test_4242",
                        PaymentMethodKind.CreditCard,
                        DisplayName: "Sandbox Test Card",
                        MaskedLast4Hint: "4242",
                        CardBrandHint: "Visa",
                        ExpiryMonthHint: 12,
                        ExpiryYearHint: 2030,
                        SetAsDefault: true
                    )
                )
            ).Value;

            return (accountId, testGwId, liveGw.Id, testMethod.Id);
        }
    }

    private static async Task VerifySandboxAndPendingLivePaymentGatesBlockLiveChargesAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid accountA,
        Guid testGwA,
        Guid liveGwA,
        Guid testVaultMethodA
    )
    {
        foreach (
            var nonLiveStage in new[]
            {
                TenantLifecycleStage.Sandbox,
                TenantLifecycleStage.PendingVerification,
            }
        )
        {
            using (AmbientTenantContext.BeginScope(tenantA, "alpha-aid", nonLiveStage))
            {
                await using var scope = provider.CreateAsyncScope();
                var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
                var donorId = Guid.CreateVersion7();

                var testCharge = await commands.SendAsync<
                    RecordFinancialTransactionCommand,
                    FinancialTransactionResponse
                >(
                    new RecordFinancialTransactionCommand(
                        donorId,
                        "USD",
                        FinancialTransactionSourceType.Website,
                        FinancialTenderType.CreditCard,
                        [new FinancialTransactionSplitSpec(accountA, 25.00m)],
                        FinancialGatewayId: testGwA,
                        OneTimeHostedToken: "tok_test_ok"
                    )
                );
                Assert.True(testCharge.IsSuccess);

                var blockedCharge = await commands.SendAsync<
                    RecordFinancialTransactionCommand,
                    FinancialTransactionResponse
                >(
                    new RecordFinancialTransactionCommand(
                        donorId,
                        "USD",
                        FinancialTransactionSourceType.Website,
                        FinancialTenderType.CreditCard,
                        [new FinancialTransactionSplitSpec(accountA, 50.00m)],
                        FinancialGatewayId: liveGwA,
                        OneTimeHostedToken: "tok_live_blocked"
                    )
                );
                Assert.True(blockedCharge.IsFailure);
                Assert.Equal(
                    "Finance.LiveChargeRequiresApprovedTenant",
                    blockedCharge.PrimaryError.Code
                );

                var blockedVault = await commands.SendAsync<
                    VaultSavedPaymentMethodCommand,
                    SavedPaymentMethodResponse
                >(
                    new VaultSavedPaymentMethodCommand(
                        donorId,
                        liveGwA,
                        "tok_live_vault_blocked",
                        PaymentMethodKind.CreditCard,
                        DisplayName: "Blocked Live Card",
                        MaskedLast4Hint: "1111",
                        CardBrandHint: "Visa",
                        ExpiryMonthHint: 10,
                        ExpiryYearHint: 2030
                    )
                );
                Assert.Equal(
                    "Finance.LiveChargeRequiresApprovedTenant",
                    blockedVault.PrimaryError.Code
                );

                var blockedRecurring = await commands.SendAsync<
                    CreateRecurringScheduleCommand,
                    RecurringScheduleResponse
                >(
                    new CreateRecurringScheduleCommand(
                        donorId,
                        liveGwA,
                        testVaultMethodA,
                        RecurringSchedulePurpose.CharitableContribution,
                        PledgeFrequency.Monthly,
                        "USD",
                        new DateOnly(2026, 12, 1),
                        [new RecurringScheduleSplitSpec(accountA, 35.00m)]
                    )
                );
                Assert.Equal(
                    "Finance.LiveChargeRequiresApprovedTenant",
                    blockedRecurring.PrimaryError.Code
                );
            }
        }
    }
}
