using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        _ = builder.ToTable("devices");
        _ = builder.HasKey(x => x.Id);
        ConfigureIdentityAndNetwork(builder);
        ConfigurePrintAndKiosk(builder);
        ConfigureAuditAndRelations(builder);
    }

    private static void ConfigureIdentityAndNetwork(EntityTypeBuilder<Device> builder)
    {
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        _ = builder
            .Property(x => x.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(50)
            .IsRequired();
        _ = builder.HasIndex(x => x.NormalizedName).IsUnique();
        _ = builder
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder
            .Property(x => x.DeviceTypeValueId)
            .HasColumnName("device_type_value_id")
            .IsRequired();
        _ = builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        _ = builder
            .Property(x => x.NormalizedIpAddress)
            .HasColumnName("normalized_ip_address")
            .HasMaxLength(45);
        _ = builder
            .HasIndex(x => new { x.DeviceTypeValueId, x.NormalizedIpAddress })
            .IsUnique()
            .HasFilter("normalized_ip_address IS NOT NULL");
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.LocationId).HasColumnName("location_id");
    }

    private static void ConfigurePrintAndKiosk(EntityTypeBuilder<Device> builder)
    {
        _ = builder
            .Property(x => x.PrintFrom)
            .HasColumnName("print_from")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder
            .Property(x => x.PrintToOverride)
            .HasColumnName("print_to_override")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(x => x.PrinterDeviceId).HasColumnName("printer_device_id");
        _ = builder.Property(x => x.ProxyDeviceId).HasColumnName("proxy_device_id");
        _ = builder.Property(x => x.HasCamera).HasColumnName("has_camera").IsRequired();
        _ = builder
            .Property(x => x.CameraBarcodeConfigurationType)
            .HasColumnName("camera_barcode_configuration_type")
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder
            .Property(x => x.KioskType)
            .HasColumnName("kiosk_type")
            .HasConversion<string>()
            .HasMaxLength(32);
    }

    private static void ConfigureAuditAndRelations(EntityTypeBuilder<Device> builder)
    {
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();
        _ = builder
            .HasMany(x => x.Locations)
            .WithOne()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(x => x.Locations).UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
