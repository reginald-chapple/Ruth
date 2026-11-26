using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class SiteDomainConfiguration : IEntityTypeConfiguration<SiteDomain>
{
    public void Configure(EntityTypeBuilder<SiteDomain> builder)
    {
        _ = builder.ToTable("site_domains");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Domain).HasMaxLength(255).IsRequired();
        _ = builder.HasIndex(static x => x.Domain).IsUnique();
        _ = builder.HasIndex(static x => x.SiteId);
    }
}
