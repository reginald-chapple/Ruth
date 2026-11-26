using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Bookings;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class BookingSlotHoldEntityConfiguration : IEntityTypeConfiguration<BookingSlotHold>
{
    public void Configure(EntityTypeBuilder<BookingSlotHold> builder)
    {
        _ = builder.ToTable("booking_slot_holds");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.HoldToken).HasMaxLength(40).IsRequired();
        _ = builder
            .Property(static x => x.TargetKind)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.HasIndex(static x => x.HoldToken).IsUnique();
        _ = builder.HasIndex(static x => new { x.IsCompleted, x.ExpiresAtUtc });
        _ = builder.HasIndex(static x => new { x.EffectiveBlockStartUtc, x.EffectiveBlockEndUtc });
    }
}
