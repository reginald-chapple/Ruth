using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class NoteAttachmentConfiguration : IEntityTypeConfiguration<NoteAttachment>
{
    public void Configure(EntityTypeBuilder<NoteAttachment> builder)
    {
        _ = builder.ToTable("note_attachments");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.NoteId).HasColumnName("note_id").IsRequired();
        _ = builder.Property(x => x.BinaryFileId).HasColumnName("binary_file_id").IsRequired();

        _ = builder.HasIndex(x => new { x.NoteId, x.BinaryFileId }).IsUnique();
    }
}
