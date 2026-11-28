using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Tests.Fixtures;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Infrastructure;

[Collection(CommercePostgresFixtures.Name)]
public sealed class W7S6CommerceMultiTenantRlsIntegrationTests(
    CommercePostgresContainerFixture postgres
)
{
    [Fact]
    public async Task ForceRls_CollidingCatalogSkus_TenantPrefixedDigitalFiles_AndSandboxLiveCheckoutGate_IsolateTenants()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CommerceTestHostFactory.BuildAndMigrateAsync(dbConn);

        await W7S6CommerceRlsTestHelper.AssertAllCommerceTablesHaveForceRlsAndFailClosedAsync(
            dbConn,
            provider
        );

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var variantA = await SeedTenantCatalogAndVerifySandboxGateAsync(provider, tenantA);
        await VerifyLiveTenantACheckoutSucceedsAsync(provider, tenantA, variantA);
        await W7S6CommerceRlsTestHelper.VerifyTenantBCollidingSkuAndZeroCrossTenantLeakageAsync(
            provider,
            tenantB,
            variantA
        );
    }

    private static async Task<Guid> SeedTenantCatalogAndVerifySandboxGateAsync(
        ServiceProvider provider,
        TenantId tenantA
    )
    {
        using (
            AmbientTenantContext.BeginScope(tenantA, "alpha-commerce", TenantLifecycleStage.Sandbox)
        )
        {
            await using var scope = provider.CreateAsyncScope();
            await W7S6CommerceRlsTestHelper.SeedTenantContributorsAsync(
                scope.ServiceProvider,
                tenantA
            );

            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            await VerifyDigitalFileKeyStartsWithTenantPrefixAsync(commands, tenantA);
            var variantId = await CreateLiveGatewayBackedProductAsync(commands, queries);
            await AssertSandboxCheckoutBlockedAsync(commands, variantId);
            return variantId;
        }
    }

    private static async Task VerifyDigitalFileKeyStartsWithTenantPrefixAsync(
        ICommandDispatcher commands,
        TenantId tenantA
    )
    {
        var fileType = (
            await commands.SendAsync<CreateBinaryFileTypeCommand, BinaryFileTypeResponse>(
                new CreateBinaryFileTypeCommand(
                    "Digital Guides",
                    AllowAnonymous: true,
                    AllowedExtensions: ".pdf"
                )
            )
        ).Value;

        await using var pdfStream = new MemoryStream(
            Encoding.UTF8.GetBytes("Tenant A PDF Handbook")
        );
        var uploadedFile = (
            await commands.SendAsync<UploadBinaryFileCommand, BinaryFileResponse>(
                new UploadBinaryFileCommand(
                    fileType.Id,
                    "handbook.pdf",
                    "application/pdf",
                    pdfStream,
                    IsTemporary: false
                )
            )
        ).Value;
        Assert.StartsWith(
            $"tenants/{tenantA}/binary-files/",
            uploadedFile.ObjectKey,
            StringComparison.Ordinal
        );
    }

    private static async Task<Guid> CreateLiveGatewayBackedProductAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries
    )
    {
        var opAccount = (
            await queries.QueryAsync<
                GetFinancialAccountsQuery,
                IReadOnlyList<FinancialAccountResponse>
            >(new GetFinancialAccountsQuery(ActiveOnly: true, IsTaxDeductible: false))
        )
            .Value[0]
            .Id;
        var charAccount = (
            await queries.QueryAsync<
                GetFinancialAccountsQuery,
                IReadOnlyList<FinancialAccountResponse>
            >(new GetFinancialAccountsQuery(ActiveOnly: true, IsTaxDeductible: true))
        )
            .Value[0]
            .Id;

        var liveGw = (
            await commands.SendAsync<CreateFinancialGatewayCommand, FinancialGatewayResponse>(
                new CreateFinancialGatewayCommand(
                    new FinancialGatewaySpec(
                        "Live Storefront Gateway",
                        "test",
                        SupportsCards: true,
                        SupportsAch: true,
                        SupportsRecurring: true,
                        IsTestMode: false,
                        IsActive: true
                    )
                )
            )
        ).Value;

        var product = (
            await commands.SendAsync<CreateProductCommand, ProductResponse>(
                new CreateProductCommand(
                    new ProductSpec(
                        "Supporter Gala Kit",
                        "supporter-gala-kit",
                        ProductKind.PhysicalPickupOrFlatRate,
                        "USD",
                        opAccount,
                        [
                            new ProductVariantSpec(
                                "GALA-KIT",
                                "Standard",
                                80m,
                                FairMarketValueAmount: 25m
                            ),
                        ],
                        IsQuidProQuo: true,
                        QuidProQuoCharitableAccountId: charAccount,
                        DefaultFinancialGatewayId: liveGw.Id
                    )
                )
            )
        ).Value;

        return product.Variants[0].Id;
    }

    private static async Task AssertSandboxCheckoutBlockedAsync(
        ICommandDispatcher commands,
        Guid variantId
    )
    {
        var blockedCheckout = await commands.SendAsync<
            SubmitCommerceOrderCommand,
            CommerceOrderResponse
        >(
            new SubmitCommerceOrderCommand(
                "Jordan Buyer",
                "jordan@alpha.org",
                [new CommerceCartItemDto(variantId, 1)],
                FulfillmentMethod: CommerceFulfillmentMethod.LocalPickup,
                OneTimeHostedPaymentToken: "tok_sandbox_live_attempt"
            )
        );
        Assert.True(blockedCheckout.IsFailure);
        Assert.Equal("Finance.LiveChargeRequiresApprovedTenant", blockedCheckout.PrimaryError.Code);
    }

    private static async Task VerifyLiveTenantACheckoutSucceedsAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid variantA
    )
    {
        using (
            AmbientTenantContext.BeginScope(tenantA, "alpha-commerce", TenantLifecycleStage.Live)
        )
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var liveOrder = await commands.SendAsync<
                SubmitCommerceOrderCommand,
                CommerceOrderResponse
            >(
                new SubmitCommerceOrderCommand(
                    "Jordan Buyer",
                    "jordan@alpha.org",
                    [new CommerceCartItemDto(variantA, 1)],
                    FulfillmentMethod: CommerceFulfillmentMethod.LocalPickup,
                    OneTimeHostedPaymentToken: "tok_live_approved_checkout"
                )
            );
            Assert.True(liveOrder.IsSuccess);
            Assert.Equal(25m, liveOrder.Value.NonDeductibleRevenueAmount);
            Assert.Equal(55m, liveOrder.Value.TaxDeductibleContributionAmount);
        }
    }
}
