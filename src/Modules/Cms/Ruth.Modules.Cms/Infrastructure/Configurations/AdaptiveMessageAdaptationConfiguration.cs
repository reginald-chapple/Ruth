using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Personalization;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class AdaptiveMessageAdaptationConfiguration
    : IEntityTypeConfiguration<AdaptiveMessageAdaptation>
{
    public void Configure(EntityTypeBuilder<AdaptiveMessageAdaptation> builder)
    {
        _ = builder.ToTable("adaptive_message_adaptations");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Headline).HasMaxLength(300).IsRequired();
        _ = builder.Property(static x => x.BodyMarkup).HasColumnType("text").IsRequired();
        _ = builder.Property(static x => x.CallToActionText).HasMaxLength(120);
        _ = builder.Property(static x => x.CallToActionUrl).HasMaxLength(1024);
        _ = builder.Property(static x => x.ImageUrl).HasMaxLength(1024);
        CmsJsonbConverters.ConfigureJsonbList(builder, static x => x.SegmentKeys, "segment_keys");
        _ = builder.HasIndex(static x => new { x.AdaptiveMessageId, x.Order });
    }
}
