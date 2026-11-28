using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

internal static class W7S6FinanceRlsTestHelper
{
    public static async Task VerifyLivePromotionAllowsLiveCaptureAndPrefixesStorageKeysAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid accountA,
        Guid liveGwA
    )
    {
        using (AmbientTenantContext.BeginScope(tenantA, "alpha-aid", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var donorId = Guid.CreateVersion7();

            var liveVault = await commands.SendAsync<
                VaultSavedPaymentMethodCommand,
                SavedPaymentMethodResponse
            >(
                new VaultSavedPaymentMethodCommand(
                    donorId,
                    liveGwA,
                    "tok_live_approved_4242",
                    PaymentMethodKind.CreditCard,
                    DisplayName: "Approved Live Visa",
                    MaskedLast4Hint: "4242",
                    CardBrandHint: "Visa",
                    ExpiryMonthHint: 11,
                    ExpiryYearHint: 2030,
                    SetAsDefault: true
                )
            );
            Assert.True(liveVault.IsSuccess);

            var liveCharge = await commands.SendAsync<
                RecordFinancialTransactionCommand,
                FinancialTransactionResponse
            >(
                new RecordFinancialTransactionCommand(
                    donorId,
                    "USD",
                    FinancialTransactionSourceType.Website,
                    FinancialTenderType.CreditCard,
                    [new FinancialTransactionSplitSpec(accountA, 150.00m)],
                    FinancialGatewayId: liveGwA,
                    SavedPaymentMethodId: liveVault.Value.Id
                )
            );
            Assert.True(liveCharge.IsSuccess);

            var checkScan = await commands.SendAsync<
                IngestScannedCheckCommand,
                CheckScannerIngestResponse
            >(
                new IngestScannedCheckCommand(
                    "021000021",
                    "4455667788",
                    "2050",
                    75.00m,
                    "USD",
                    accountA,
                    OverridePersonId: donorId,
                    BindBankIdentifierToPerson: true,
                    FrontImageBase64: Convert.ToBase64String(
                        Encoding.UTF8.GetBytes("TENANT-A-CHECK-IMAGE")
                    ),
                    ImageContentType: "image/png"
                )
            );
            Assert.True(checkScan.IsSuccess);

            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            var storedImg = Assert.Single(db.FinancialTransactionImages.ToList());
            Assert.StartsWith(
                $"tenants/{tenantA}/finance/checks/",
                storedImg.StorageObjectKey,
                StringComparison.Ordinal
            );
        }
    }

    public static async Task VerifyTenantBIsolationAndCollidingAccountCodesAsync(
        ServiceProvider provider,
        TenantId tenantB,
        Guid tenantAAccountId
    )
    {
        using (AmbientTenantContext.BeginScope(tenantB, "beta-aid", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<FinanceTenantSeedContributor>();
            await seeder.SeedTenantAsync(tenantB);

            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
            var accountsB = (
                await queries.QueryAsync<
                    GetFinancialAccountsQuery,
                    IReadOnlyList<FinancialAccountResponse>
                >(new GetFinancialAccountsQuery())
            ).Value;

            Assert.Contains(accountsB, static a =>
                string.Equals(a.Code, "GEN-FUND", StringComparison.Ordinal)
            );
            Assert.DoesNotContain(accountsB, a => a.Id == tenantAAccountId);

            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            Assert.Empty(db.FinancialTransactions.ToList());
            Assert.Empty(db.FinancialTransactionImages.ToList());
        }
    }

    public static async Task AssertAllFinanceTablesHaveForceRlsAndFailClosedAsync(
        string adminConn,
        ServiceProvider provider
    )
    {
        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity "
                + "FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace "
                + "WHERE n.nspname = 'finance' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
            await using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.True(reader.GetBoolean(1), $"Missing RLS on finance.{reader.GetString(0)}");
                Assert.True(
                    reader.GetBoolean(2),
                    $"Missing FORCE RLS on finance.{reader.GetString(0)}"
                );
            }

            Assert.True(
                count >= 24,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected >= 24 tables in finance schema, saw {count}."
                )
            );
        }

        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConn)
        );
        await appConn.OpenAsync();
        await using var appCmd = appConn.CreateCommand();
        appCmd.CommandText = "SELECT COUNT(*) FROM finance.financial_accounts";
        var unsetCount = Convert.ToInt64(
            await appCmd.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture
        );
        Assert.Equal(0L, unsetCount);
    }
}
