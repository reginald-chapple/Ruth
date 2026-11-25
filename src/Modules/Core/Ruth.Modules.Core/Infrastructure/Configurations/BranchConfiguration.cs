using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        _ = builder.ToTable("branches");
        _ = builder.HasKey(x => x.Id);
        ConfigureIdentityAndDetails(builder);
        ConfigureOperationalAndAudit(builder);
        ConfigureRelations(builder);
    }

    private static void ConfigureIdentityAndDetails(EntityTypeBuilder<Branch> builder)
    {
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength)
            .IsRequired();
        _ = builder.HasIndex(x => x.NormalizedName).IsUnique();
        _ = builder
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder.Property(x => x.ShortCode).HasColumnName("short_code").HasMaxLength(50);
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();
        _ = builder.Property(x => x.Url).HasColumnName("url").HasMaxLength(2048);
        _ = builder.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(50);
        _ = builder.Property(x => x.TimeZoneId).HasColumnName("time_zone_id").HasMaxLength(64);
        _ = builder.Property(x => x.LocationId).HasColumnName("location_id");
        _ = builder.Property(x => x.LeaderPersonId).HasColumnName("leader_person_id");
        _ = builder.Property(x => x.BranchStatusValueId).HasColumnName("branch_status_value_id");
        _ = builder.Property(x => x.BranchTypeValueId).HasColumnName("branch_type_value_id");
        _ = builder.Property(x => x.TeamGroupId).HasColumnName("team_group_id");
        _ = builder.Property(x => x.PhotoId).HasColumnName("photo_id");
    }

    private static void ConfigureOperationalAndAudit(EntityTypeBuilder<Branch> builder)
    {
        _ = builder.Property(x => x.OpenedDate).HasColumnName("opened_date");
        _ = builder.Property(x => x.ClosedDate).HasColumnName("closed_date");
        _ = builder.Property(x => x.BeaconId).HasColumnName("beacon_id");
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();
    }

    private static void ConfigureRelations(EntityTypeBuilder<Branch> builder)
    {
        _ = builder
            .HasMany(x => x.Schedules)
            .WithOne()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(x => x.Schedules).UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .HasMany(x => x.Topics)
            .WithOne()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(x => x.Topics).UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(x => x.CondensedName);
        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
