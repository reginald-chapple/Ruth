using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        _ = builder.ToTable("tags");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder.Property(x => x.EntityTypeId).HasColumnName("entity_type_id");
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder.Property(x => x.CategoryId).HasColumnName("category_id");
        _ = builder.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
        _ = builder.HasIndex(x => new
        {
            x.NormalizedName,
            x.OwnerUserId,
            x.EntityTypeId,
        });
        _ = builder.Property(x => x.IconName).HasColumnName("icon_name").HasMaxLength(100);
        _ = builder
            .Property(x => x.BackgroundColor)
            .HasColumnName("background_color")
            .HasMaxLength(64)
            .IsRequired();
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        _ = builder
            .HasMany(x => x.TaggedItems)
            .WithOne()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(x => x.TaggedItems).UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(x => x.IsPersonal);
        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
