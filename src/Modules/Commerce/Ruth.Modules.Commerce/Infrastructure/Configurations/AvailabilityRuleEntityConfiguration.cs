using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Bookings;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class AvailabilityRuleEntityConfiguration
    : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        _ = builder.ToTable("availability_rules");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder
            .Property(static x => x.TargetKind)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder
            .Property(static x => x.RuleType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.DayOfWeek).HasConversion<string>().HasMaxLength(16);
        _ = builder.Property(static x => x.TimeZoneId).HasMaxLength(80).IsRequired();

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.BookableServiceId);
        _ = builder.HasIndex(static x => x.RentableResourceId);
    }
}
