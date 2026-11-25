using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class EntitySetItemConfiguration : IEntityTypeConfiguration<EntitySetItem>
{
    public void Configure(EntityTypeBuilder<EntitySetItem> builder)
    {
        _ = builder.ToTable("entity_set_items");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.EntitySetId).HasColumnName("entity_set_id").IsRequired();
        _ = builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();
        _ = builder
            .Property(x => x.AdditionalMergeValuesJson)
            .HasColumnName("additional_merge_values_json")
            .HasColumnType("jsonb");

        _ = builder.HasIndex(x => new { x.EntitySetId, x.EntityId }).IsUnique();
    }
}
