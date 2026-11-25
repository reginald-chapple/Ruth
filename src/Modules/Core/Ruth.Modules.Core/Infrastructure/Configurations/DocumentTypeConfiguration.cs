using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Files;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        _ = builder.ToTable("document_types");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength)
            .IsRequired();
        _ = builder.Property(x => x.EntityTypeId).HasColumnName("entity_type_id").IsRequired();
        _ = builder.HasIndex(x => x.EntityTypeId);
        _ = builder
            .Property(x => x.EntityTypeQualifierColumn)
            .HasColumnName("entity_type_qualifier_column")
            .HasMaxLength(50);
        _ = builder
            .Property(x => x.EntityTypeQualifierValue)
            .HasColumnName("entity_type_qualifier_value")
            .HasMaxLength(200);
        _ = builder
            .Property(x => x.BinaryFileTypeId)
            .HasColumnName("binary_file_type_id")
            .IsRequired();
        _ = builder.Property(x => x.UserSelectable).HasColumnName("user_selectable").IsRequired();
        _ = builder.Property(x => x.IconName).HasColumnName("icon_name").HasMaxLength(100);
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();
        _ = builder
            .Property(x => x.DefaultDocumentNameTemplate)
            .HasColumnName("default_document_name_template")
            .HasMaxLength(500);
        _ = builder
            .Property(x => x.MaxDocumentsPerEntity)
            .HasColumnName("max_documents_per_entity");
        _ = builder.Property(x => x.IsImage).HasColumnName("is_image").IsRequired();
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
