using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Files;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class SignatureDocumentTemplateConfiguration
    : IEntityTypeConfiguration<SignatureDocumentTemplate>
{
    public void Configure(EntityTypeBuilder<SignatureDocumentTemplate> builder)
    {
        _ = builder.ToTable("signature_document_templates");
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
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder.Property(x => x.BinaryFileTypeId).HasColumnName("binary_file_type_id");
        _ = builder.Property(x => x.DocumentTerm).HasColumnName("document_term").HasMaxLength(100);
        _ = builder.Property(x => x.TemplateMarkup).HasColumnName("template_markup");
        _ = builder
            .Property(x => x.SignatureType)
            .HasColumnName("signature_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder
            .Property(x => x.IsValidInFuture)
            .HasColumnName("is_valid_in_future")
            .IsRequired();
        _ = builder
            .Property(x => x.ValidityDurationInDays)
            .HasColumnName("validity_duration_in_days");
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
