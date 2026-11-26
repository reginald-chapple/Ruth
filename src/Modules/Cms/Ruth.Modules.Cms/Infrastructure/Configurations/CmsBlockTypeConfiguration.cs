using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class CmsBlockTypeConfiguration : IEntityTypeConfiguration<CmsBlockType>
{
    public void Configure(EntityTypeBuilder<CmsBlockType> builder)
    {
        _ = builder.ToTable("block_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Key).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Category).HasMaxLength(80).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.DefaultSettings,
            "default_settings",
            createGinIndex: false
        );
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Key).IsUnique();
    }
}
