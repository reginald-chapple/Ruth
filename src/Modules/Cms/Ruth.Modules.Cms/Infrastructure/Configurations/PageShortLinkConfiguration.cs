using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class PageShortLinkConfiguration : IEntityTypeConfiguration<PageShortLink>
{
    public void Configure(EntityTypeBuilder<PageShortLink> builder)
    {
        _ = builder.ToTable("page_short_links");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Token).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.TargetUrl).HasMaxLength(2048).IsRequired();
        _ = builder.Property(static x => x.UtmSource).HasMaxLength(120);
        _ = builder.Property(static x => x.UtmMedium).HasMaxLength(120);
        _ = builder.Property(static x => x.UtmCampaign).HasMaxLength(120);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Token).IsUnique();
        _ = builder.HasIndex(static x => x.SiteId);
    }
}
