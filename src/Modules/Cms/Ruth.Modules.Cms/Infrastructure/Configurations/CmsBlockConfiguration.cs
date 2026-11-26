using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class CmsBlockConfiguration : IEntityTypeConfiguration<CmsBlock>
{
    public void Configure(EntityTypeBuilder<CmsBlock> builder)
    {
        _ = builder.ToTable("blocks");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Zone).HasMaxLength(80).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.PreHtml).HasColumnType("text");
        _ = builder.Property(static x => x.PostHtml).HasColumnType("text");
        _ = builder.Property(static x => x.CssClass).HasMaxLength(160);
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.Settings,
            "settings",
            createGinIndex: false
        );
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            "attribute_values",
            createGinIndex: true
        );
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.PageId);
        _ = builder.HasIndex(static x => x.LayoutId);
        _ = builder.HasIndex(static x => x.SiteId);
    }
}
