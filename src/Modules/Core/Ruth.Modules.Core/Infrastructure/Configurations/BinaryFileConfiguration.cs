using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Files;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class BinaryFileConfiguration : IEntityTypeConfiguration<BinaryFile>
{
    public void Configure(EntityTypeBuilder<BinaryFile> builder)
    {
        _ = builder.ToTable("binary_files");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder
            .Property(x => x.BinaryFileTypeId)
            .HasColumnName("binary_file_type_id")
            .IsRequired();
        _ = builder.HasIndex(x => x.BinaryFileTypeId);
        _ = builder
            .Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();
        _ = builder
            .Property(x => x.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(255)
            .IsRequired();
        _ = builder.Property(x => x.FileSize).HasColumnName("file_size").IsRequired();
        _ = builder
            .Property(x => x.StorageProviderKey)
            .HasColumnName("storage_provider_key")
            .HasMaxLength(64)
            .IsRequired();
        _ = builder
            .Property(x => x.ObjectKey)
            .HasColumnName("object_key")
            .HasMaxLength(1024)
            .IsRequired();
        _ = builder.Property(x => x.Width).HasColumnName("width");
        _ = builder.Property(x => x.Height).HasColumnName("height");
        _ = builder.Property(x => x.IsTemporary).HasColumnName("is_temporary").IsRequired();
        _ = builder.HasIndex(x => new { x.IsTemporary, x.ModifiedOnUtc });
        _ = builder.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();
        _ = builder
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder.Property(x => x.ParentEntityTypeId).HasColumnName("parent_entity_type_id");
        _ = builder.Property(x => x.ParentEntityId).HasColumnName("parent_entity_id");
        _ = builder.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id");
        _ = builder
            .Property(x => x.ContentLastModifiedOnUtc)
            .HasColumnName("content_last_modified_on_utc");
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        _ = builder.Ignore(x => x.IsImage);
        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
