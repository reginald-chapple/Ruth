using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        _ = builder.ToTable("locations");
        _ = builder.HasKey(x => x.Id);
        ConfigureIdentityAndAddress(builder);
        ConfigureGeoAndRoomThresholds(builder);
        ConfigureVerificationAndAudit(builder);
    }

    private static void ConfigureIdentityAndAddress(EntityTypeBuilder<Location> builder)
    {
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder.Property(x => x.ParentLocationId).HasColumnName("parent_location_id");
        _ = builder.HasIndex(x => x.ParentLocationId);
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength);
        _ = builder
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.LocationTypeValueId).HasColumnName("location_type_value_id");
        _ = builder.Property(x => x.ImageId).HasColumnName("image_id");
        _ = builder.Property(x => x.Street1).HasColumnName("street1").HasMaxLength(100);
        _ = builder.Property(x => x.Street2).HasColumnName("street2").HasMaxLength(100);
        _ = builder.Property(x => x.City).HasColumnName("city").HasMaxLength(50);
        _ = builder.Property(x => x.County).HasColumnName("county").HasMaxLength(50);
        _ = builder.Property(x => x.State).HasColumnName("state").HasMaxLength(50);
        _ = builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(50);
        _ = builder.Property(x => x.Country).HasColumnName("country").HasMaxLength(50);
    }

    private static void ConfigureGeoAndRoomThresholds(EntityTypeBuilder<Location> builder)
    {
        _ = builder
            .Property(x => x.GeoPoint)
            .HasColumnName("geo_point")
            .HasColumnType("geography(Point, 4326)");
        _ = builder
            .Property(x => x.GeoFence)
            .HasColumnName("geo_fence")
            .HasColumnType("geography(Polygon, 4326)");
        _ = builder.HasIndex(x => x.GeoPoint).HasMethod("gist");
        _ = builder.HasIndex(x => x.GeoFence).HasMethod("gist");
        _ = builder
            .Property(x => x.IsGeoPointLocked)
            .HasColumnName("is_geo_point_locked")
            .IsRequired();
        _ = builder.Property(x => x.Barcode).HasColumnName("barcode").HasMaxLength(40);
        _ = builder
            .Property(x => x.AssessorParcelId)
            .HasColumnName("assessor_parcel_id")
            .HasMaxLength(50);
        _ = builder.Property(x => x.PrinterDeviceId).HasColumnName("printer_device_id");
        _ = builder.Property(x => x.SoftRoomThreshold).HasColumnName("soft_room_threshold");
        _ = builder.Property(x => x.FirmRoomThreshold).HasColumnName("firm_room_threshold");
        _ = builder.Property(x => x.BeaconId).HasColumnName("beacon_id");
    }

    private static void ConfigureVerificationAndAudit(EntityTypeBuilder<Location> builder)
    {
        _ = builder
            .Property(x => x.StandardizeAttemptedOnUtc)
            .HasColumnName("standardize_attempted_on_utc");
        _ = builder
            .Property(x => x.StandardizeAttemptedServiceType)
            .HasColumnName("standardize_attempted_service_type")
            .HasMaxLength(50);
        _ = builder
            .Property(x => x.StandardizeAttemptedResult)
            .HasColumnName("standardize_attempted_result")
            .HasMaxLength(200);
        _ = builder.Property(x => x.StandardizedOnUtc).HasColumnName("standardized_on_utc");
        _ = builder
            .Property(x => x.GeocodeAttemptedOnUtc)
            .HasColumnName("geocode_attempted_on_utc");
        _ = builder
            .Property(x => x.GeocodeAttemptedServiceType)
            .HasColumnName("geocode_attempted_service_type")
            .HasMaxLength(50);
        _ = builder
            .Property(x => x.GeocodeAttemptedResult)
            .HasColumnName("geocode_attempted_result")
            .HasMaxLength(200);
        _ = builder.Property(x => x.GeocodedOnUtc).HasColumnName("geocoded_on_utc");
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();
        _ = builder.Ignore(x => x.IsNamedLocation);
        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
