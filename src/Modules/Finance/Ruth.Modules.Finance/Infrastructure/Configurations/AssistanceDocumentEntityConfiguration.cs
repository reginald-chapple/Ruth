using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Assistance;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class AssistanceDocumentEntityConfiguration
    : IEntityTypeConfiguration<AssistanceDocument>
{
    public void Configure(EntityTypeBuilder<AssistanceDocument> builder)
    {
        _ = builder.ToTable("assistance_documents");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.CaseId).IsRequired();
        _ = builder.Property(static x => x.Title).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.DocumentCategory).HasMaxLength(80).IsRequired();

        _ = builder.HasIndex(static x => x.CaseId);
    }
}
