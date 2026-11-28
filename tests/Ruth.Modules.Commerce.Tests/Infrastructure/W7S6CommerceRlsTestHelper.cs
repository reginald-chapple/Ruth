using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Infrastructure;

internal static class W7S6CommerceRlsTestHelper
{
    public static async Task SeedTenantContributorsAsync(
        IServiceProvider serviceProvider,
        TenantId tenantId
    )
    {
        foreach (
            var contributor in serviceProvider
                .GetServices<ITenantSeedContributor>()
                .OrderBy(static c => c.Order)
        )
        {
            await contributor.SeedTenantAsync(tenantId);
        }
    }

    public static async Task VerifyTenantBCollidingSkuAndZeroCrossTenantLeakageAsync(
        ServiceProvider provider,
        TenantId tenantB,
        Guid tenantAVariantId
    )
    {
        using (AmbientTenantContext.BeginScope(tenantB, "beta-commerce", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            await SeedTenantContributorsAsync(scope.ServiceProvider, tenantB);

            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var existingB = (
                await queries.QueryAsync<GetProductsQuery, IReadOnlyList<ProductResponse>>(
                    new GetProductsQuery(ActiveOnly: false)
                )
            ).Value;
            Assert.Empty(existingB);

            var crossAttempt = await commands.SendAsync<
                SubmitCommerceOrderCommand,
                CommerceOrderResponse
            >(
                new SubmitCommerceOrderCommand(
                    "Cross Tenant Buyer",
                    "buyer@beta.org",
                    [new CommerceCartItemDto(tenantAVariantId, 1)],
                    FulfillmentMethod: CommerceFulfillmentMethod.LocalPickup
                )
            );
            Assert.True(crossAttempt.IsFailure);

            var opAccountB = (
                await queries.QueryAsync<
                    GetFinancialAccountsQuery,
                    IReadOnlyList<FinancialAccountResponse>
                >(new GetFinancialAccountsQuery(ActiveOnly: true, IsTaxDeductible: false))
            )
                .Value[0]
                .Id;

            var collidingProdB = await commands.SendAsync<CreateProductCommand, ProductResponse>(
                new CreateProductCommand(
                    new ProductSpec(
                        "Beta Gala Kit",
                        "supporter-gala-kit",
                        ProductKind.PhysicalPickupOrFlatRate,
                        "USD",
                        opAccountB,
                        [new ProductVariantSpec("GALA-KIT", "Beta Variant", 40m)]
                    )
                )
            );
            Assert.True(collidingProdB.IsSuccess);
        }
    }

    public static async Task AssertAllCommerceTablesHaveForceRlsAndFailClosedAsync(
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
                + "WHERE n.nspname = 'commerce' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
            await using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.True(reader.GetBoolean(1), $"Missing RLS on commerce.{reader.GetString(0)}");
                Assert.True(
                    reader.GetBoolean(2),
                    $"Missing FORCE RLS on commerce.{reader.GetString(0)}"
                );
            }

            Assert.True(
                count >= 10,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected >= 10 tables in commerce schema, saw {count}."
                )
            );
        }

        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConn)
        );
        await appConn.OpenAsync();
        await using var appCmd = appConn.CreateCommand();
        appCmd.CommandText = "SELECT COUNT(*) FROM commerce.products";
        var unsetCount = Convert.ToInt64(
            await appCmd.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture
        );
        Assert.Equal(0L, unsetCount);
    }
}
