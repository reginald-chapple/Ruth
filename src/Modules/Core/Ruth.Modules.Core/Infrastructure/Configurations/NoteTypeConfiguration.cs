using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class NoteTypeConfiguration : IEntityTypeConfiguration<NoteType>
{
    public void Configure(EntityTypeBuilder<NoteType> builder)
    {
        _ = builder.ToTable("note_types");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder.Property(x => x.EntityTypeId).HasColumnName("entity_type_id").IsRequired();
        _ = builder.HasIndex(x => x.EntityTypeId);
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength)
            .IsRequired();
        _ = builder.Property(x => x.UserSelectable).HasColumnName("user_selectable").IsRequired();
        _ = builder.Property(x => x.IconName).HasColumnName("icon_name").HasMaxLength(100);
        _ = builder.Property(x => x.Color).HasColumnName("color").HasMaxLength(64);
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();
        _ = builder
            .Property(x => x.RequiresApprovals)
            .HasColumnName("requires_approvals")
            .IsRequired();
        _ = builder.Property(x => x.AllowsWatching).HasColumnName("allows_watching").IsRequired();
        _ = builder.Property(x => x.AllowsReplies).HasColumnName("allows_replies").IsRequired();
        _ = builder.Property(x => x.MaxReplyDepth).HasColumnName("max_reply_depth");
        _ = builder
            .Property(x => x.AutoWatchAuthors)
            .HasColumnName("auto_watch_authors")
            .IsRequired();
        _ = builder
            .Property(x => x.AllowsAttachments)
            .HasColumnName("allows_attachments")
            .IsRequired();
        _ = builder.Property(x => x.BinaryFileTypeId).HasColumnName("binary_file_type_id");
        _ = builder
            .Property(x => x.IsMentionEnabled)
            .HasColumnName("is_mention_enabled")
            .IsRequired();
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
