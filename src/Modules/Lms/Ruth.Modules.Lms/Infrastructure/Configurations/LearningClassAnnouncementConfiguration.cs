using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Classes;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningClassAnnouncementConfiguration
    : IEntityTypeConfiguration<LearningClassAnnouncement>
{
    public void Configure(EntityTypeBuilder<LearningClassAnnouncement> builder)
    {
        _ = builder.ToTable("learning_class_announcements");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Title).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(500);
        _ = builder.Property(static x => x.DescriptionHtml).IsRequired();
        _ = builder.HasIndex(static x => new { x.LearningClassId, x.PublishDateTimeUtc });
    }
}
