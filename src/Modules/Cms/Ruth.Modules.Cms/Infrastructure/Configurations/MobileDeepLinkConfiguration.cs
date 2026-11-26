using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Channels;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class MobileDeepLinkConfiguration : IEntityTypeConfiguration<MobileDeepLink>
{
    public void Configure(EntityTypeBuilder<MobileDeepLink> builder)
    {
        _ = builder.ToTable("mobile_deep_links");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.RoutePrefix).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.FallbackUrl).HasMaxLength(1024).IsRequired();
        _ = builder.HasIndex(static x => x.MobileApplicationId);
    }
}
