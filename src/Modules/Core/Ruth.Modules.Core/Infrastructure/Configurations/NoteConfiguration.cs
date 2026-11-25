using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        _ = builder.ToTable("notes");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.NoteTypeId).HasColumnName("note_type_id").IsRequired();
        _ = builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        _ = builder.HasIndex(x => new { x.EntityId, x.NoteTypeId });
        _ = builder.Property(x => x.ParentNoteId).HasColumnName("parent_note_id");
        _ = builder.HasIndex(x => x.ParentNoteId);
        _ = builder.Property(x => x.Caption).HasColumnName("caption").HasMaxLength(200);
        _ = builder.Property(x => x.Text).HasColumnName("text").IsRequired();
        _ = builder.Property(x => x.IsAlert).HasColumnName("is_alert").IsRequired();
        _ = builder.Property(x => x.IsPinned).HasColumnName("is_pinned").IsRequired();
        _ = builder.Property(x => x.IsPrivateNote).HasColumnName("is_private_note").IsRequired();
        _ = builder
            .Property(x => x.ApprovalStatus)
            .HasColumnName("approval_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(x => x.ApprovedOnUtc).HasColumnName("approved_on_utc");
        _ = builder.Property(x => x.AuthorUserId).HasColumnName("author_user_id");
        _ = builder.Property(x => x.EditedOnUtc).HasColumnName("edited_on_utc");
        _ = builder.Property(x => x.EditedByUserId).HasColumnName("edited_by_user_id");
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        _ = builder
            .HasMany(x => x.Attachments)
            .WithOne()
            .HasForeignKey(x => x.NoteId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(x => x.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
