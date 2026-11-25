using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class EntitySetConfiguration : IEntityTypeConfiguration<EntitySet>
{
    public void Configure(EntityTypeBuilder<EntitySet> builder)
    {
        _ = builder.ToTable("entity_sets");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.ParentEntitySetId).HasColumnName("parent_entity_set_id");
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength);
        _ = builder.Property(x => x.EntityTypeId).HasColumnName("entity_type_id");
        _ = builder.Property(x => x.ExpireOnUtc).HasColumnName("expire_on_utc");
        _ = builder.HasIndex(x => x.ExpireOnUtc);
        _ = builder.Property(x => x.PurposeValueId).HasColumnName("purpose_value_id");
        _ = builder
            .Property(x => x.Note)
            .HasColumnName("note")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");

        _ = builder
            .HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.EntitySetId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
