using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class CmsThemeConfiguration : IEntityTypeConfiguration<CmsTheme>
{
    public void Configure(EntityTypeBuilder<CmsTheme> builder)
    {
        _ = builder.ToTable("themes");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.CustomCss).HasColumnType("text");
        JsonbAttributeConventions.ConfigureJsonbStringMap(
            builder,
            static x => x.CssCustomProperties,
            "css_custom_properties"
        );
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
