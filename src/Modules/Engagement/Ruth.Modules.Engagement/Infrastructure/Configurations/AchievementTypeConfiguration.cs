using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Achievements;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class AchievementTypeConfiguration : IEntityTypeConfiguration<AchievementType>
{
    public void Configure(EntityTypeBuilder<AchievementType> builder)
    {
        _ = builder.ToTable("achievement_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Code).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder
            .Property(static x => x.SourceKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.BadgeIconCssClass).HasMaxLength(100);
        _ = builder.Property(static x => x.HighlightColorHex).HasMaxLength(16);
        _ = builder.Property(static x => x.CelebrationSystemCommunicationKey).HasMaxLength(120);

        EngagementJsonbConventions.ConfigureList(
            builder,
            static x => x.PrerequisiteAchievementIds,
            "prerequisite_achievement_ids"
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Code).IsUnique();
    }
}
