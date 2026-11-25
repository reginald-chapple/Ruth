using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class DefinedValueConfiguration : IEntityTypeConfiguration<DefinedValue>
{
    public void Configure(EntityTypeBuilder<DefinedValue> builder)
    {
        _ = builder.ToTable("defined_values");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.DefinedTypeId).HasColumnName("defined_type_id").IsRequired();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder
            .Property(x => x.Value)
            .HasColumnName("value")
            .HasMaxLength(CoreTextPolicy.DefinedValueMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder.Property(x => x.CategoryId).HasColumnName("category_id");
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();
        _ = builder.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.Attributes,
            columnName: "attributes",
            createGinIndex: true
        );

        _ = builder.HasIndex(x => new { x.DefinedTypeId, x.Order });
        _ = builder.Ignore(x => x.AttributeValues);
        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
