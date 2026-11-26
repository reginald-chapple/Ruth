using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class CmsPageConfiguration : IEntityTypeConfiguration<CmsPage>
{
    public void Configure(EntityTypeBuilder<CmsPage> builder)
    {
        _ = builder.ToTable("pages");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.InternalName).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.PageTitle).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.BrowserTitle).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.IconCssClass).HasMaxLength(120);
        _ = builder
            .Property(static x => x.DisplayInNavWhen)
            .HasConversion<string>()
            .HasMaxLength(32);
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            "attribute_values",
            createGinIndex: true
        );
        _ = builder
            .HasMany(static x => x.Routes)
            .WithOne()
            .HasForeignKey(static r => r.PageId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .HasMany(static x => x.Contexts)
            .WithOne()
            .HasForeignKey(static c => c.PageId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Routes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .Navigation(static x => x.Contexts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.SiteId, x.Order });
    }
}
