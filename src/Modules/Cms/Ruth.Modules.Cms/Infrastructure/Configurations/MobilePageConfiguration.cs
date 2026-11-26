using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Channels;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class MobilePageConfiguration : IEntityTypeConfiguration<MobilePage>
{
    public void Configure(EntityTypeBuilder<MobilePage> builder)
    {
        _ = builder.ToTable("mobile_pages");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Title).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.PageKey).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Route).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.IconName).HasMaxLength(80);
        _ = builder.Property(static x => x.AdaptiveMessageKey).HasMaxLength(128);
        _ = builder.Property(static x => x.BlocksJson).HasColumnType("jsonb").IsRequired();
        _ = builder.HasIndex(static x => new { x.MobileApplicationId, x.PageKey }).IsUnique();
    }
}
