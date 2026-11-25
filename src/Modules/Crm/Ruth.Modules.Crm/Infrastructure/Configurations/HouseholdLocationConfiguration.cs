using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Households;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class HouseholdLocationConfiguration : IEntityTypeConfiguration<HouseholdLocation>
{
    public void Configure(EntityTypeBuilder<HouseholdLocation> builder)
    {
        _ = builder.ToTable("household_locations");
        _ = builder.HasKey(static l => l.Id);

        _ = builder.Property(static l => l.LocationType).HasConversion<string>().HasMaxLength(24);
        _ = builder.Property(static l => l.AddressSummary).HasMaxLength(300);

        _ = builder.HasIndex(static l => new { l.HouseholdId, l.LocationId }).IsUnique();
        _ = builder.HasIndex(static l => l.LocationId);
    }
}
