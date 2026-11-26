using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Orders;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class CommerceOrderEntityConfiguration : IEntityTypeConfiguration<CommerceOrder>
{
    public void Configure(EntityTypeBuilder<CommerceOrder> builder)
    {
        _ = builder.ToTable("orders");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.OrderNumber).HasMaxLength(36).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.BuyerName).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.BuyerEmail).HasMaxLength(254).IsRequired();
        _ = builder
            .Property(static x => x.FulfillmentMethod)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.ShippingAddressSummary).HasMaxLength(500);
        _ = builder.Property(static x => x.TrackingReference).HasMaxLength(160);
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();

        _ = builder.Property(static x => x.SubtotalAmount).HasPrecision(18, 2).IsRequired();
        _ = builder
            .Property(static x => x.MemberDiscountTotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();
        _ = builder.Property(static x => x.TaxTotalAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.ShippingFeeAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        _ = builder
            .Property(static x => x.NonDeductibleRevenueAmount)
            .HasPrecision(18, 2)
            .IsRequired();
        _ = builder
            .Property(static x => x.TaxDeductibleContributionAmount)
            .HasPrecision(18, 2)
            .IsRequired();
        _ = builder.Property(static x => x.PaidAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.RefundedAmount).HasPrecision(18, 2).IsRequired();

        _ = builder
            .HasMany(static x => x.Lines)
            .WithOne()
            .HasForeignKey(static x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(static x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.OrderNumber).IsUnique();
        _ = builder.HasIndex(static x => x.BuyerPersonId);
        _ = builder.HasIndex(static x => new { x.Status, x.CreatedOnUtc });
    }
}
