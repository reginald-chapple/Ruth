using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class NoteWatchConfiguration : IEntityTypeConfiguration<NoteWatch>
{
    public void Configure(EntityTypeBuilder<NoteWatch> builder)
    {
        _ = builder.ToTable("note_watches");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.WatcherUserId).HasColumnName("watcher_user_id").IsRequired();
        _ = builder.HasIndex(x => x.WatcherUserId);
        _ = builder.Property(x => x.IsWatching).HasColumnName("is_watching").IsRequired();
        _ = builder.Property(x => x.WatchReplies).HasColumnName("watch_replies").IsRequired();
        _ = builder.Property(x => x.AllowOverride).HasColumnName("allow_override").IsRequired();
        _ = builder.Property(x => x.NoteTypeId).HasColumnName("note_type_id");
        _ = builder.Property(x => x.EntityTypeId).HasColumnName("entity_type_id");
        _ = builder.Property(x => x.EntityId).HasColumnName("entity_id");
        _ = builder.Property(x => x.NoteId).HasColumnName("note_id");
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
    }
}
