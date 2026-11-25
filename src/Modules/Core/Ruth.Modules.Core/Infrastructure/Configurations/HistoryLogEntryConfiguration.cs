using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class HistoryLogEntryConfiguration : IEntityTypeConfiguration<HistoryLogEntry>
{
    public void Configure(EntityTypeBuilder<HistoryLogEntry> builder)
    {
        _ = builder.ToTable("history_log_entries");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.EntityTypeId).HasColumnName("entity_type_id").IsRequired();
        _ = builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        _ = builder.HasIndex(x => new
        {
            x.EntityTypeId,
            x.EntityId,
            x.OccurredOnUtc,
        });
        _ = builder.Property(x => x.CategoryId).HasColumnName("category_id");
        _ = builder.Property(x => x.Verb).HasColumnName("verb").HasMaxLength(50).IsRequired();
        _ = builder
            .Property(x => x.ChangeType)
            .HasColumnName("change_type")
            .HasMaxLength(20)
            .IsRequired();
        _ = builder.Property(x => x.Caption).HasColumnName("caption").HasMaxLength(200);
        _ = builder.Property(x => x.ValueName).HasColumnName("value_name").HasMaxLength(250);
        _ = builder.Property(x => x.OldValue).HasColumnName("old_value");
        _ = builder.Property(x => x.NewValue).HasColumnName("new_value");
        _ = builder.Property(x => x.IsSensitive).HasColumnName("is_sensitive").IsRequired();
        _ = builder.Property(x => x.RelatedEntityTypeId).HasColumnName("related_entity_type_id");
        _ = builder.Property(x => x.RelatedEntityId).HasColumnName("related_entity_id");
        _ = builder.Property(x => x.RelatedData).HasColumnName("related_data").HasMaxLength(500);
        _ = builder
            .Property(x => x.SourceOfChange)
            .HasColumnName("source_of_change")
            .HasMaxLength(200);
        _ = builder.Property(x => x.ChangedByUserId).HasColumnName("changed_by_user_id");
        _ = builder.Property(x => x.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");

        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
