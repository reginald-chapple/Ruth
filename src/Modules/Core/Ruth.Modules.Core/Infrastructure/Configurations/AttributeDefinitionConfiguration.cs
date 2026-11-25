using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class AttributeDefinitionConfiguration
    : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
    {
        _ = builder.ToTable("attribute_definitions");
        _ = builder.HasKey(x => x.Id);
        ConfigureKeyAndScopeProperties(builder);
        ConfigureMetadataAndAuditProperties(builder);
    }

    private static void ConfigureKeyAndScopeProperties(
        EntityTypeBuilder<AttributeDefinition> builder
    )
    {
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder.Property(x => x.EntityTypeId).HasColumnName("entity_type_id");
        _ = builder
            .Property(x => x.EntityTypeQualifierColumn)
            .HasColumnName("entity_type_qualifier_column")
            .HasMaxLength(100);
        _ = builder
            .Property(x => x.EntityTypeQualifierValue)
            .HasColumnName("entity_type_qualifier_value")
            .HasMaxLength(200);
        _ = builder
            .Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(CoreTextPolicy.AttributeKeyMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.NormalizedKey)
            .HasColumnName("normalized_key")
            .HasMaxLength(CoreTextPolicy.AttributeKeyMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder
            .Property(x => x.FieldTypeKey)
            .HasColumnName("field_type_key")
            .HasMaxLength(64)
            .IsRequired();
    }

    private static void ConfigureMetadataAndAuditProperties(
        EntityTypeBuilder<AttributeDefinition> builder
    )
    {
        _ = builder.Property(x => x.IsRequired).HasColumnName("is_required").IsRequired();
        _ = builder.Property(x => x.IsGridColumn).HasColumnName("is_grid_column").IsRequired();
        _ = builder.Property(x => x.IsMultiValue).HasColumnName("is_multi_value").IsRequired();
        _ = builder.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.DefaultValue).HasColumnName("default_value");
        _ = builder.Property(x => x.GlobalValue).HasColumnName("global_value");
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();
        _ = builder.Property(x => x.CategoryId).HasColumnName("category_id");
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        JsonbAttributeConventions.ConfigureJsonbStringMap(
            builder,
            static x => x.Qualifiers,
            columnName: "qualifiers"
        );

        _ = builder.HasIndex(x => new
        {
            x.EntityTypeId,
            x.EntityTypeQualifierColumn,
            x.EntityTypeQualifierValue,
            x.NormalizedKey,
        });
        _ = builder.Ignore(x => x.IsGlobal);
        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
