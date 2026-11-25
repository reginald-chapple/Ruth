using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class TaggedItemConfiguration : IEntityTypeConfiguration<TaggedItem>
{
    public void Configure(EntityTypeBuilder<TaggedItem> builder)
    {
        _ = builder.ToTable("tagged_items");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.TagId).HasColumnName("tag_id").IsRequired();
        _ = builder.Property(x => x.EntityTypeId).HasColumnName("entity_type_id").IsRequired();
        _ = builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        _ = builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");

        _ = builder
            .HasIndex(x => new
            {
                x.TagId,
                x.EntityTypeId,
                x.EntityId,
            })
            .IsUnique();
        _ = builder.HasIndex(x => new { x.EntityTypeId, x.EntityId });
    }
}
