using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.Modules.Commerce.Domain.Orders;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.Modules.Commerce.Domain.PunchPasses;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Domain;

public sealed class W4S9CatalogAndQuidProQuoDomainTests
{
    private static readonly DateTimeOffset _nowUtc = new(2026, 11, 25, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _today = new(2026, 11, 25);

    [Fact]
    public void QuidProQuoEngine_SplitsCommercialFmvTaxShipping_FromTaxDeductibleExcess_AndCapsWhenDiscountDropsBelowFmv()
    {
        var revAcc = Guid.CreateVersion7();
        var charAcc = Guid.CreateVersion7();
        var prodRes = Product.Create(
            new ProductSpec(
                "Annual Gala Ticket & Merch",
                "annual-gala-merch",
                ProductKind.PhysicalPickupOrFlatRate,
                "USD",
                revAcc,
                [
                    new ProductVariantSpec(
                        "GALA-100",
                        "Benefactor Seat",
                        UnitPrice: 100.00m,
                        FairMarketValueAmount: 35.00m,
                        TrackInventory: true,
                        StockQuantity: 10
                    ),
                ],
                IsQuidProQuo: true,
                QuidProQuoCharitableAccountId: charAcc,
                IsTaxable: true,
                FlatTaxRatePercent: 10.0m,
                AllowLocalPickup: true,
                AllowFlatRateShipping: true,
                FlatShippingFeeAmount: 5.00m
            )
        );
        Assert.True(prodRes.IsSuccess);
        var product = prodRes.Value;
        var variant = product.Variants.Single();
        var lookup = new Dictionary<Guid, (Product, ProductVariant)>
        {
            [variant.Id.Value] = (product, variant),
        };

        var goldRule = MemberPricingRule
            .Create(
                new MemberPricingRuleSpec(
                    "Gold 20% Off",
                    MemberDiscountKind.Percentage,
                    20m,
                    RequiredMembershipTier: "Gold",
                    Priority: 10
                )
            )
            .Value;
        var silverRule = MemberPricingRule
            .Create(
                new MemberPricingRuleSpec(
                    "Silver $10 Off",
                    MemberDiscountKind.FixedAmountPerUnit,
                    10m,
                    RequiredMembershipTier: "Gold",
                    Priority: 5
                )
            )
            .Value;

        var tiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Gold" };
        var planRes = CommercePricingAndSplitEngine.Evaluate(
            [new CommerceCartItemDto(variant.Id.Value, 2)],
            lookup,
            [silverRule, goldRule],
            tiers,
            new HashSet<Guid>(),
            new HashSet<Guid>(),
            CommerceFulfillmentMethod.FlatRateShipping,
            new FlatRateCommerceTaxCalculator(),
            taxRateOverridePercent: null,
            _today
        );

        Assert.True(planRes.IsSuccess);
        var plan = planRes.Value;
        Assert.Equal(200.00m, plan.ListSubtotalAmount);
        Assert.Equal(40.00m, plan.MemberDiscountTotalAmount);
        Assert.Equal(160.00m, plan.NetSubtotalAmount);
        Assert.Equal(7.00m, plan.TaxTotalAmount);
        Assert.Equal(10.00m, plan.ShippingFeeAmount);
        Assert.Equal(177.00m, plan.GrandTotalAmount);
        Assert.Equal(87.00m, plan.NonDeductibleRevenueAmount);
        Assert.Equal(90.00m, plan.TaxDeductibleContributionAmount);
        Assert.Equal(2, plan.AccountSplits.Count);

        var patronRule = MemberPricingRule
            .Create(
                new MemberPricingRuleSpec(
                    "Fellowship 80% Off",
                    MemberDiscountKind.Percentage,
                    80m,
                    RequiredMembershipTier: "Fellow"
                )
            )
            .Value;
        var fellowPlan = CommercePricingAndSplitEngine
            .Evaluate(
                [new CommerceCartItemDto(variant.Id.Value, 1)],
                lookup,
                [patronRule],
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Fellow" },
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                CommerceFulfillmentMethod.LocalPickup,
                new FlatRateCommerceTaxCalculator(),
                taxRateOverridePercent: null,
                _today
            )
            .Value;

        Assert.Equal(20.00m, fellowPlan.NetSubtotalAmount);
        Assert.Equal(0m, fellowPlan.TaxDeductibleContributionAmount);
        Assert.Equal(22.00m, fellowPlan.NonDeductibleRevenueAmount);
    }

    [Fact]
    public void Product_And_Order_EnforceInvariants_FulfillmentTransitions_AndRefunds()
    {
        var sameAcc = Guid.CreateVersion7();
        var invalidQpq = Product.Create(
            new ProductSpec(
                "Bad QPQ",
                "bad-qpq",
                ProductKind.PhysicalPickupOrFlatRate,
                "USD",
                sameAcc,
                [new ProductVariantSpec("SKU-1", "Std", 50m, FairMarketValueAmount: 20m)],
                IsQuidProQuo: true,
                QuidProQuoCharitableAccountId: sameAcc
            )
        );
        Assert.True(invalidQpq.IsFailure);
        Assert.Equal(CommerceErrors.QuidProQuoAccountInvalid.Code, invalidQpq.PrimaryError.Code);

        var digitalMissingFile = Product.Create(
            new ProductSpec(
                "Bad Digital",
                "bad-digital",
                ProductKind.DigitalDownload,
                "USD",
                sameAcc,
                [new ProductVariantSpec("DIG-1", "PDF", 15m)]
            )
        );
        Assert.True(digitalMissingFile.IsFailure);

        var prod = Product
            .Create(
                new ProductSpec(
                    "Member Hoodie",
                    "member-hoodie",
                    ProductKind.PhysicalPickupOrFlatRate,
                    "USD",
                    sameAcc,
                    [
                        new ProductVariantSpec(
                            "HOOD-L",
                            "Large",
                            45m,
                            TrackInventory: true,
                            StockQuantity: 2
                        ),
                    ],
                    AllowLocalPickup: true,
                    AllowFlatRateShipping: false
                )
            )
            .Value;
        var variant = prod.Variants.Single();
        Assert.True(variant.DeductStock(3).IsFailure);
        Assert.True(variant.DeductStock(2).IsSuccess);
        Assert.Equal(0, variant.StockQuantity);
        variant.Restock(2);

        var lookup = new Dictionary<Guid, (Product, ProductVariant)>
        {
            [variant.Id.Value] = (prod, variant),
        };
        var plan = CommercePricingAndSplitEngine
            .Evaluate(
                [new CommerceCartItemDto(variant.Id.Value, 1)],
                lookup,
                [],
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                CommerceFulfillmentMethod.LocalPickup,
                new FlatRateCommerceTaxCalculator(),
                taxRateOverridePercent: null,
                _today
            )
            .Value;

        var order = CommerceOrder
            .Create(
                Guid.CreateVersion7(),
                "Sam Buyer",
                "sam@example.org",
                branchId: null,
                shippingAddressSummary: null,
                plan,
                _nowUtc
            )
            .Value;
        order.MarkPaid(Guid.CreateVersion7(), 45m);
        Assert.Equal(CommerceOrderStatus.Paid, order.Status);

        Assert.True(order.AdvanceFulfillment(CommerceOrderStatus.Shipped, "TRK-1").IsFailure);
        Assert.True(
            order.AdvanceFulfillment(CommerceOrderStatus.ReadyForPickup, "BIN-2").IsSuccess
        );
        Assert.True(
            order
                .AdvanceFulfillment(CommerceOrderStatus.Fulfilled, trackingReference: null)
                .IsSuccess
        );

        Assert.True(order.RecordRefund(15m).IsSuccess);
        Assert.Equal(CommerceOrderStatus.PartiallyRefunded, order.Status);
        Assert.True(order.RecordRefund(30m).IsSuccess);
        Assert.Equal(CommerceOrderStatus.Refunded, order.Status);
    }

    [Fact]
    public void IssuedPunchPass_EnforcesHouseholdSharing_BranchScope_Expiry_Adjustments_AndReversal()
    {
        var branchA = Guid.CreateVersion7();
        var branchB = Guid.CreateVersion7();
        var owner = Guid.CreateVersion7();
        var spouse = Guid.CreateVersion7();
        var stranger = Guid.CreateVersion7();

        var passType = PunchPassType
            .Create(
                new PunchPassTypeSpec(
                    "5-Visit Pool Pass",
                    "POOL-5",
                    5,
                    ValidityDays: 30,
                    AllowedBranchId: branchA,
                    AllowHouseholdSharing: true
                )
            )
            .Value;

        var pass = IssuedPunchPass
            .Issue(passType, owner, packMultiplier: 1, sourceOrderId: null, "Init", _nowUtc)
            .Value;
        Assert.Equal(5, pass.RemainingCredits);

        Assert.True(
            pass.Redeem(
                1,
                owner,
                beneficiaryIsHouseholdMember: true,
                branchB,
                "Wrong branch",
                _nowUtc
            ).IsFailure
        );
        Assert.True(
            pass.Redeem(
                1,
                stranger,
                beneficiaryIsHouseholdMember: false,
                branchA,
                "Stranger",
                _nowUtc
            ).IsFailure
        );
        Assert.True(
            pass.Redeem(
                2,
                spouse,
                beneficiaryIsHouseholdMember: true,
                branchA,
                "Spouse swim",
                _nowUtc
            ).IsSuccess
        );
        Assert.Equal(3, pass.RemainingCredits);

        Assert.True(pass.AdjustBalance(1, "Courtesy", _nowUtc).IsSuccess);
        Assert.Equal(4, pass.RemainingCredits);

        Assert.True(
            pass.Redeem(
                1,
                owner,
                beneficiaryIsHouseholdMember: true,
                branchA,
                "Late",
                _nowUtc.AddDays(31)
            ).IsFailure
        );
        Assert.Equal(IssuedPunchPassStatus.Expired, pass.Status);

        var reversed = pass.RevokeForRefund("Order refund", _nowUtc);
        Assert.Equal(4, reversed);
        Assert.Equal(0, pass.RemainingCredits);
        Assert.Equal(IssuedPunchPassStatus.Revoked, pass.Status);
    }
}
