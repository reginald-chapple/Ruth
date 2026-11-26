using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Orders;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class CommerceOrderLineEntityConfiguration
    : IEntityTypeConfiguration<CommerceOrderLine>
{
    public void Configure(EntityTypeBuilder<CommerceOrderLine> builder)
    {
        _ = builder.ToTable("order_lines");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Id).ValueGeneratedNever();
        _ = builder.Property(static x => x.Sku).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.ProductName).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.VariantName).HasMaxLength(160).IsRequired();
        _ = builder
            .Property(static x => x.ProductKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.ListUnitPrice).HasPrecision(18, 2).IsRequired();
        _ = builder
            .Property(static x => x.UnitMemberDiscountAmount)
            .HasPrecision(18, 2)
            .IsRequired();
        _ = builder.Property(static x => x.NetUnitPrice).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.FairMarketValuePerUnit).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.LineSubtotalAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.LineTaxAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.LineShippingFeeAmount).HasPrecision(18, 2).IsRequired();
        _ = builder
            .Property(static x => x.NonDeductibleRevenueAmount)
            .HasPrecision(18, 2)
            .IsRequired();
        _ = builder
            .Property(static x => x.TaxDeductibleContributionAmount)
            .HasPrecision(18, 2)
            .IsRequired();
        _ = builder.Property(static x => x.AppliedMemberPricingRuleName).HasMaxLength(160);
        _ = builder.HasIndex(static x => x.OrderId);
    }
}
