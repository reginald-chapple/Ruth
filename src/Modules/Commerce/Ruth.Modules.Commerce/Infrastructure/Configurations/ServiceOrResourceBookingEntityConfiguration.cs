using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Bookings;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class ServiceOrResourceBookingEntityConfiguration
    : IEntityTypeConfiguration<ServiceOrResourceBooking>
{
    public void Configure(EntityTypeBuilder<ServiceOrResourceBooking> builder)
    {
        _ = builder.ToTable("service_bookings");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.ConfirmationCode).HasMaxLength(36).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder
            .Property(static x => x.TargetKind)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.TargetName).HasMaxLength(200).IsRequired();
        _ = builder
            .Property(static x => x.DeliveryMode)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.CustomerName).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.CustomerEmail).HasMaxLength(254).IsRequired();
        _ = builder.Property(static x => x.CustomerPhone).HasMaxLength(40);
        _ = builder.Property(static x => x.CustomerNotes).HasMaxLength(1000);
        _ = builder
            .Property(static x => x.PricingKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.ListAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.DiscountAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.PaidAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.CancellationReason).HasMaxLength(500);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.ConfirmationCode).IsUnique();
        _ = builder.HasIndex(static x => x.CustomerPersonId);
        _ = builder.HasIndex(static x => new
        {
            x.Status,
            x.EffectiveBlockStartUtc,
            x.EffectiveBlockEndUtc,
        });
    }
}
