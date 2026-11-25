using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Files;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        _ = builder.ToTable("documents");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.DocumentTypeId).HasColumnName("document_type_id").IsRequired();
        _ = builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        _ = builder.HasIndex(x => new { x.DocumentTypeId, x.EntityId });
        _ = builder.Property(x => x.BinaryFileId).HasColumnName("binary_file_id").IsRequired();
        _ = builder.HasIndex(x => x.BinaryFileId).IsUnique();
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength)
            .IsRequired();
        _ = builder.Property(x => x.PurposeKey).HasColumnName("purpose_key").HasMaxLength(100);
        _ = builder
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");

        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
