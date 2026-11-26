using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class CmsSiteConfiguration : IEntityTypeConfiguration<CmsSite>
{
    public void Configure(EntityTypeBuilder<CmsSite> builder)
    {
        _ = builder.ToTable("sites");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.SiteType).HasConversion<string>().HasMaxLength(24);
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            "attribute_values",
            createGinIndex: true
        );
        _ = builder
            .HasMany(static x => x.Domains)
            .WithOne()
            .HasForeignKey(static d => d.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Domains)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
