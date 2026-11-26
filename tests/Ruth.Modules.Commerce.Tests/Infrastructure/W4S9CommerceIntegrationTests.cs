using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Tests.Fixtures;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Infrastructure;

[Collection(CommercePostgresFixtures.Name)]
public sealed class W4S9CommerceIntegrationTests(CommercePostgresContainerFixture postgres)
{
    [Fact]
    public async Task QuidProQuoSplits_GroupAndWalletDiscounts_DigitalDownload_PunchPassWalletSync_AndRefund_WorkEndToEnd()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CommerceTestHostFactory.BuildAndMigrateAsync(dbConn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var seed = await CommerceTestSeedHelper.SeedAsync(commands, queries);

        var (qpqVariantId, digitalVariantId, packVariantId) = await CreateCatalogAndRulesAsync(
            commands,
            seed
        );

        var order = await VerifyMemberCheckoutAndFinanceSplitsAsync(
            commands,
            queries,
            seed,
            qpqVariantId,
            digitalVariantId,
            packVariantId
        );

        await W4S9CommerceVerificationHelper.VerifyDigitalLinkPunchPassAndRefundAsync(
            commands,
            queries,
            seed,
            order
        );
        await W4S9CommerceVerificationHelper.VerifyInvoiceCheckoutAsync(
            commands,
            queries,
            seed,
            qpqVariantId
        );
    }

    private static async Task<(
        Guid QpqVariantId,
        Guid DigitalVariantId,
        Guid PackVariantId
    )> CreateCatalogAndRulesAsync(ICommandDispatcher commands, CommerceSeedBundle seed)
    {
        var passType = (
            await commands.SendAsync<CreatePunchPassTypeCommand, PunchPassTypeResponse>(
                new CreatePunchPassTypeCommand(
                    new PunchPassTypeSpec(
                        "10-Visit Facility Pack",
                        "FAC-10",
                        10,
                        ValidityDays: 180,
                        AllowHouseholdSharing: true
                    )
                )
            )
        ).Value;

        _ = await commands.SendAsync<CreateMemberPricingRuleCommand, MemberPricingRuleResponse>(
            new CreateMemberPricingRuleCommand(
                new MemberPricingRuleSpec(
                    "Gold Wallet Pass 10% Off",
                    MemberDiscountKind.Percentage,
                    10m,
                    RequiredMembershipTier: "Gold"
                )
            )
        );
        _ = await commands.SendAsync<CreateMemberPricingRuleCommand, MemberPricingRuleResponse>(
            new CreateMemberPricingRuleCommand(
                new MemberPricingRuleSpec(
                    "Sustaining Chapter 25% Off",
                    MemberDiscountKind.Percentage,
                    25m,
                    RequiredGroupId: seed.GroupId
                )
            )
        );

        var qpqProd = (
            await commands.SendAsync<CreateProductCommand, ProductResponse>(
                new CreateProductCommand(
                    new ProductSpec(
                        "Gala Supporter Kit",
                        "gala-supporter-kit",
                        ProductKind.PhysicalPickupOrFlatRate,
                        "USD",
                        seed.OperatingAccountId,
                        [
                            new ProductVariantSpec(
                                "GALA-KIT",
                                "Standard Kit",
                                100m,
                                FairMarketValueAmount: 30m,
                                TrackInventory: true,
                                StockQuantity: 5
                            ),
                        ],
                        IsQuidProQuo: true,
                        QuidProQuoCharitableAccountId: seed.CharitableAccountId,
                        IsTaxable: true,
                        FlatTaxRatePercent: 10m,
                        AllowLocalPickup: true,
                        AllowFlatRateShipping: true,
                        FlatShippingFeeAmount: 8m,
                        DefaultFinancialGatewayId: seed.GatewayId
                    )
                )
            )
        ).Value;

        var digProd = (
            await commands.SendAsync<CreateProductCommand, ProductResponse>(
                new CreateProductCommand(
                    new ProductSpec(
                        "Digital Handbook",
                        "digital-handbook",
                        ProductKind.DigitalDownload,
                        "USD",
                        seed.OperatingAccountId,
                        [
                            new ProductVariantSpec(
                                "DIG-HDBK",
                                "PDF Download",
                                20m,
                                DigitalBinaryFileId: seed.DigitalBinaryFileId
                            ),
                        ],
                        DefaultFinancialGatewayId: seed.GatewayId
                    )
                )
            )
        ).Value;

        var packProd = (
            await commands.SendAsync<CreateProductCommand, ProductResponse>(
                new CreateProductCommand(
                    new ProductSpec(
                        "Facility 10-Pack",
                        "facility-10-pack",
                        ProductKind.PunchPassPack,
                        "USD",
                        seed.OperatingAccountId,
                        [
                            new ProductVariantSpec(
                                "FAC-PACK-10",
                                "10 Visits",
                                80m,
                                PunchPassTypeId: passType.Id
                            ),
                        ],
                        DefaultFinancialGatewayId: seed.GatewayId
                    )
                )
            )
        ).Value;

        return (qpqProd.Variants[0].Id, digProd.Variants[0].Id, packProd.Variants[0].Id);
    }

    private static async Task<CommerceOrderResponse> VerifyMemberCheckoutAndFinanceSplitsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        CommerceSeedBundle seed,
        Guid qpqVariantId,
        Guid digitalVariantId,
        Guid packVariantId
    )
    {
        var orderRes = await commands.SendAsync<SubmitCommerceOrderCommand, CommerceOrderResponse>(
            new SubmitCommerceOrderCommand(
                "Nora Chen",
                "nora.chen@example.org",
                [
                    new CommerceCartItemDto(qpqVariantId, 1),
                    new CommerceCartItemDto(digitalVariantId, 1),
                    new CommerceCartItemDto(packVariantId, 1),
                ],
                BuyerPersonId: seed.MemberPersonId,
                FulfillmentMethod: CommerceFulfillmentMethod.FlatRateShipping,
                ShippingAddressSummary: "450 Civic Plaza, Denver, CO",
                OneTimeHostedPaymentToken: "tok_w4s9_member_4242"
            )
        );
        Assert.True(orderRes.IsSuccess);
        var order = orderRes.Value;

        Assert.Equal(161.00m, order.TotalAmount);
        Assert.Equal(116.00m, order.NonDeductibleRevenueAmount);
        Assert.Equal(45.00m, order.TaxDeductibleContributionAmount);
        var txId = Assert.NotNull(order.LinkedFinancialTransactionId);

        var txRes = await queries.QueryAsync<
            GetFinancialTransactionByIdQuery,
            FinancialTransactionResponse
        >(new GetFinancialTransactionByIdQuery(txId));
        Assert.True(txRes.IsSuccess);
        Assert.Equal(2, txRes.Value.Splits.Count);
        Assert.Contains(txRes.Value.Splits, s =>
            s.AccountId == seed.OperatingAccountId && s.Amount == 116.00m
        );
        Assert.Contains(txRes.Value.Splits, s =>
            s.AccountId == seed.CharitableAccountId && s.Amount == 45.00m
        );

        return order;
    }
}
