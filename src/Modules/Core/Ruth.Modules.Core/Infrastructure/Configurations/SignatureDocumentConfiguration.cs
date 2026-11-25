using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain.Files;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class SignatureDocumentConfiguration : IEntityTypeConfiguration<SignatureDocument>
{
    public void Configure(EntityTypeBuilder<SignatureDocument> builder)
    {
        _ = builder.ToTable("signature_documents");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder
            .Property(x => x.SignatureDocumentTemplateId)
            .HasColumnName("signature_document_template_id")
            .IsRequired();
        _ = builder.HasIndex(x => x.SignatureDocumentTemplateId);
        _ = builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(250).IsRequired();
        _ = builder.Property(x => x.DocumentKey).HasColumnName("document_key").HasMaxLength(200);
        _ = builder
            .Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(x => x.AppliesToEntityId).HasColumnName("applies_to_entity_id");
        _ = builder.HasIndex(x => x.AppliesToEntityId);
        _ = builder.Property(x => x.AssignedToUserId).HasColumnName("assigned_to_user_id");
        _ = builder.Property(x => x.BinaryFileId).HasColumnName("binary_file_id");
        _ = builder.Property(x => x.SignedByUserId).HasColumnName("signed_by_user_id");
        _ = builder.Property(x => x.SignedName).HasColumnName("signed_name").HasMaxLength(250);
        _ = builder
            .Property(x => x.SignedByEmail)
            .HasColumnName("signed_by_email")
            .HasMaxLength(254);
        _ = builder.Property(x => x.SignedDocumentText).HasColumnName("signed_document_text");
        _ = builder
            .Property(x => x.SignedClientIp)
            .HasColumnName("signed_client_ip")
            .HasMaxLength(128);
        _ = builder
            .Property(x => x.SignedClientUserAgent)
            .HasColumnName("signed_client_user_agent")
            .HasMaxLength(512);
        _ = builder.Property(x => x.SignedOnUtc).HasColumnName("signed_on_utc");
        _ = builder.Property(x => x.SignatureData).HasColumnName("signature_data");
        _ = builder
            .Property(x => x.SignatureVerificationHash)
            .HasColumnName("signature_verification_hash")
            .HasMaxLength(128);
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        _ = builder.Ignore(x => x.IsSealed);
        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
