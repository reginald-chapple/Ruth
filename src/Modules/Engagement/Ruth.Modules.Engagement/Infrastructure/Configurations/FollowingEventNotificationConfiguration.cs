using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Following;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class FollowingEventNotificationConfiguration
    : IEntityTypeConfiguration<FollowingEventNotification>
{
    public void Configure(EntityTypeBuilder<FollowingEventNotification> builder)
    {
        _ = builder.ToTable("following_event_notifications");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.EntityTypeKey).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.EventTypeKey).HasMaxLength(100).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(1000).IsRequired();
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.DigestProcessedAtUtc, x.OccurredAtUtc });
    }
}
