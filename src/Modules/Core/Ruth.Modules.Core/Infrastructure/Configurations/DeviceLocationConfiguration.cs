using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class DeviceLocationConfiguration : IEntityTypeConfiguration<DeviceLocation>
{
    public void Configure(EntityTypeBuilder<DeviceLocation> builder)
    {
        _ = builder.ToTable("device_locations");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.DeviceId).HasColumnName("device_id").IsRequired();
        _ = builder.Property(x => x.LocationId).HasColumnName("location_id").IsRequired();

        _ = builder.HasIndex(x => new { x.DeviceId, x.LocationId }).IsUnique();
    }
}
