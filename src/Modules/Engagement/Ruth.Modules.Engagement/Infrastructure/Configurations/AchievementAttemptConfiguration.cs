using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Achievements;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class AchievementAttemptConfiguration : IEntityTypeConfiguration<AchievementAttempt>
{
    public void Configure(EntityTypeBuilder<AchievementAttempt> builder)
    {
        _ = builder.ToTable("achievement_attempts");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Progress).HasPrecision(10, 4).IsRequired();
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.PersonId, x.AchievementTypeId });
    }
}
