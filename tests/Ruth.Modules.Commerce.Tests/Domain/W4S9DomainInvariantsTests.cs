using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.Modules.Commerce.Domain.Orders;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.Modules.Commerce.Domain.PunchPasses;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Domain;

public sealed class W4S9DomainInvariantsTests
{
    private static readonly DateTimeOffset _nowUtc = new(2026, 12, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PunchPass_Product_MemberRule_AndCommerceOrder_CoverFulfillment_Adjustments_Stock_AndRepoint()
    {
        var branchA = Guid.CreateVersion7();
        var owner = Guid.CreateVersion7();
        var mergedOwner = Guid.CreateVersion7();
        var stranger = Guid.CreateVersion7();

        Assert.True(PunchPassType.Create(new PunchPassTypeSpec("", "BAD", 0)).IsFailure);
        var passType = PunchPassType
            .Create(
                new PunchPassTypeSpec(
                    "Branch 2-Pack",
                    "BR-2",
                    2,
                    ValidityDays: 30,
                    AllowedBranchId: branchA,
                    AllowHouseholdSharing: false
                )
            )
            .Value;

        var pass = IssuedPunchPass
            .Issue(passType, owner, 1, sourceOrderId: null, "Grant", _nowUtc)
            .Value;
        Assert.True(pass.IsUsableAt(_nowUtc.AddDays(1)));
        Assert.False(pass.IsUsableAt(_nowUtc.AddDays(35)));
        Assert.True(
            pass.Redeem(
                1,
                stranger,
                beneficiaryIsHouseholdMember: false,
                branchA,
                referenceNotes: null,
                _nowUtc
            ).IsFailure
        );
        Assert.True(
            pass.Redeem(
                1,
                owner,
                beneficiaryIsHouseholdMember: false,
                Guid.CreateVersion7(),
                referenceNotes: null,
                _nowUtc
            ).IsFailure
        );
        Assert.True(
            pass.Redeem(
                2,
                owner,
                beneficiaryIsHouseholdMember: false,
                branchA,
                "Deplete",
                _nowUtc
            ).IsSuccess
        );
        Assert.Equal(IssuedPunchPassStatus.Depleted, pass.Status);
        Assert.False(pass.IsUsableAt(_nowUtc));

        Assert.True(pass.AdjustBalance(3, "Courtesy top-up", _nowUtc).IsSuccess);
        Assert.Equal(IssuedPunchPassStatus.Active, pass.Status);
        Assert.True(pass.AdjustBalance(0, "", _nowUtc).IsFailure);
        Assert.True(pass.RepointPerson(owner, mergedOwner));
        Assert.Equal(3, pass.RevokeForRefund("Order cancelled", _nowUtc));
        Assert.Equal(0, pass.RevokeForRefund("Idempotent", _nowUtc));

        VerifyOrderPickupFulfillmentAndCartValidation(mergedOwner);
    }

    private static void VerifyOrderPickupFulfillmentAndCartValidation(Guid buyerId)
    {
        var revAcc = Guid.CreateVersion7();
        Assert.True(
            Product
                .Create(
                    new ProductSpec(
                        "Empty Variants",
                        "empty-variants",
                        ProductKind.PhysicalPickupOrFlatRate,
                        "USD",
                        revAcc,
                        []
                    )
                )
                .IsFailure
        );
        Assert.True(
            Product
                .Create(
                    new ProductSpec(
                        "Dup Sku",
                        "dup-sku",
                        ProductKind.PhysicalPickupOrFlatRate,
                        "USD",
                        revAcc,
                        [
                            new ProductVariantSpec("DUP-1", "One", 10m),
                            new ProductVariantSpec("dup-1", "Two", 10m),
                        ]
                    )
                )
                .IsFailure
        );

        var prod = Product
            .Create(
                new ProductSpec(
                    "Member Tote",
                    "member-tote",
                    ProductKind.PhysicalPickupOrFlatRate,
                    "USD",
                    revAcc,
                    [
                        new ProductVariantSpec(
                            "TOTE-1",
                            "Canvas Tote",
                            25m,
                            TrackInventory: true,
                            StockQuantity: 2
                        ),
                    ],
                    AllowLocalPickup: true,
                    AllowFlatRateShipping: false
                )
            )
            .Value;
        var variant = prod.Variants.First();
        Assert.True(variant.DeductStock(1).IsSuccess);
        variant.Restock(1);

        var fixedRule = MemberPricingRule
            .Create(
                new MemberPricingRuleSpec(
                    "$5 Off Tote",
                    MemberDiscountKind.FixedAmountPerUnit,
                    5m,
                    RequiredMembershipTier: "Bronze",
                    AppliesToProductId: prod.Id.Value,
                    AppliesToProductKind: ProductKind.PhysicalPickupOrFlatRate
                )
            )
            .Value;

        var lookup = new Dictionary<Guid, (Product, ProductVariant)>
        {
            [variant.Id.Value] = (prod, variant),
        };
        var tax = new FlatRateCommerceTaxCalculator();

        Assert.True(
            CommercePricingAndSplitEngine
                .Evaluate(
                    [],
                    lookup,
                    [],
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<Guid>(),
                    new HashSet<Guid>(),
                    CommerceFulfillmentMethod.LocalPickup,
                    tax,
                    taxRateOverridePercent: null,
                    DateOnly.FromDateTime(_nowUtc.DateTime)
                )
                .IsFailure
        );
        Assert.True(
            CommercePricingAndSplitEngine
                .Evaluate(
                    [new CommerceCartItemDto(Guid.Empty, 0)],
                    lookup,
                    [],
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<Guid>(),
                    new HashSet<Guid>(),
                    CommerceFulfillmentMethod.LocalPickup,
                    tax,
                    taxRateOverridePercent: null,
                    DateOnly.FromDateTime(_nowUtc.DateTime)
                )
                .IsFailure
        );

        Assert.True(
            CommercePricingAndSplitEngine
                .Evaluate(
                    [new CommerceCartItemDto(variant.Id.Value, 5)],
                    lookup,
                    [],
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<Guid>(),
                    new HashSet<Guid>(),
                    CommerceFulfillmentMethod.LocalPickup,
                    tax,
                    taxRateOverridePercent: null,
                    DateOnly.FromDateTime(_nowUtc.DateTime)
                )
                .IsFailure
        );

        var plan = CommercePricingAndSplitEngine
            .Evaluate(
                [
                    new CommerceCartItemDto(variant.Id.Value, 1),
                    new CommerceCartItemDto(variant.Id.Value, 1),
                ],
                lookup,
                [fixedRule],
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Bronze" },
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                CommerceFulfillmentMethod.LocalPickup,
                tax,
                taxRateOverridePercent: null,
                DateOnly.FromDateTime(_nowUtc.DateTime)
            )
            .Value;
        Assert.Equal(40m, plan.GrandTotalAmount);

        var order = CommerceOrder
            .Create(
                buyerId,
                "Jordan",
                "jordan@example.org",
                branchId: null,
                shippingAddressSummary: null,
                plan,
                _nowUtc
            )
            .Value;
        order.MarkPaid(Guid.CreateVersion7(), 40m);
        Assert.True(order.IsEligibleForDigitalDownload());
        Assert.True(
            order.AdvanceFulfillment(CommerceOrderStatus.ReadyForPickup, "BIN-4").IsSuccess
        );
        Assert.True(
            order
                .AdvanceFulfillment(CommerceOrderStatus.Fulfilled, trackingReference: null)
                .IsSuccess
        );
        Assert.True(order.RecordRefund(10m).IsSuccess);
        Assert.Equal(CommerceOrderStatus.PartiallyRefunded, order.Status);
        Assert.True(order.RepointPerson(buyerId, Guid.CreateVersion7()));
    }
}
