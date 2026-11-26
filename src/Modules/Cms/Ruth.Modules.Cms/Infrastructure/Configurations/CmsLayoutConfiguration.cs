using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class CmsLayoutConfiguration : IEntityTypeConfiguration<CmsLayout>
{
    public void Configure(EntityTypeBuilder<CmsLayout> builder)
    {
        _ = builder.ToTable("layouts");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.FileName).HasMaxLength(120).IsRequired();
        CmsJsonbConverters.ConfigureJsonbList(builder, static x => x.Zones, "zones");
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.SiteId, x.Slug }).IsUnique();
    }
}
