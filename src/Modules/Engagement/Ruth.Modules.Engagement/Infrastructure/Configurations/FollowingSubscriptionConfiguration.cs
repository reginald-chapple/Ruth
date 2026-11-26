using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Following;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class FollowingSubscriptionConfiguration
    : IEntityTypeConfiguration<FollowingSubscription>
{
    public void Configure(EntityTypeBuilder<FollowingSubscription> builder)
    {
        _ = builder.ToTable("following_subscriptions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.EntityTypeKey).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.EntityDisplayName).HasMaxLength(250).IsRequired();
        _ = builder.Property(static x => x.PurposeKey).HasMaxLength(64).IsRequired();
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder
            .HasIndex(static x => new
            {
                x.FollowerPersonId,
                x.EntityTypeKey,
                x.EntityId,
                x.PurposeKey,
            })
            .IsUnique();
    }
}
