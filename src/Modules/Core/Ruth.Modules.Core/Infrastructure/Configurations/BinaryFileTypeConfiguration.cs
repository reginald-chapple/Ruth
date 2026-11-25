using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Files;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class BinaryFileTypeConfiguration : IEntityTypeConfiguration<BinaryFileType>
{
    public void Configure(EntityTypeBuilder<BinaryFileType> builder)
    {
        _ = builder.ToTable("binary_file_types");
        _ = builder.HasKey(x => x.Id);

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
        _ = builder.Property(x => x.IconName).HasColumnName("icon_name").HasMaxLength(100);
        _ = builder
            .Property(x => x.StorageProviderKey)
            .HasColumnName("storage_provider_key")
            .HasMaxLength(64)
            .IsRequired();
        _ = builder
            .Property(x => x.RequiresViewSecurity)
            .HasColumnName("requires_view_security")
            .IsRequired();
        _ = builder.Property(x => x.AllowAnonymous).HasColumnName("allow_anonymous").IsRequired();
        _ = builder
            .Property(x => x.PreferredRequired)
            .HasColumnName("preferred_required")
            .IsRequired();
        _ = builder.Property(x => x.MaxFileSizeBytes).HasColumnName("max_file_size_bytes");
        _ = builder.Property(x => x.MaxWidth).HasColumnName("max_width");
        _ = builder.Property(x => x.MaxHeight).HasColumnName("max_height");
        _ = builder
            .Property(x => x.AllowedExtensions)
            .HasColumnName("allowed_extensions")
            .HasMaxLength(500);
        _ = builder
            .Property(x => x.CacheControlHeaderSettings)
            .HasColumnName("cache_control_header_settings")
            .HasMaxLength(500);
        _ = builder.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
