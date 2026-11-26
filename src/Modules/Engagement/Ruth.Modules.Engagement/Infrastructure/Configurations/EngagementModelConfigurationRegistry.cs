using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal static class EngagementModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        ApplyW5S1Configurations(modelBuilder);
        ApplyW5S2Configurations(modelBuilder);
        ApplyW5S3Configurations(modelBuilder);
        ApplyW5S4Configurations(modelBuilder);
    }

    private static void ApplyW5S1Configurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new PipelineTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PipelineStageConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PipelineActivityTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new EngagementOpportunityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new EngagementRequestConfiguration());
        _ = modelBuilder.ApplyConfiguration(new EngagementRequestActivityConfiguration());
    }

    private static void ApplyW5S2Configurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new JourneyProgramConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MilestoneStatusConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MilestoneTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonMilestoneConfiguration());
        _ = modelBuilder.ApplyConfiguration(new JourneyCompletionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new StreakTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new StreakExclusionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new StreakConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AchievementTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AchievementAttemptConfiguration());
    }

    private static void ApplyW5S3Configurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new SupportCategoryConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SupportRequestConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SupportRequestCommentConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ReminderTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ReminderConfiguration());
        _ = modelBuilder.ApplyConfiguration(new NotificationMessageConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FollowingSubscriptionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FollowingEventNotificationConfiguration());
    }

    private static void ApplyW5S4Configurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new ConstituentContactConfiguration());
        _ = modelBuilder.ApplyConfiguration(new TouchpointConfiguration());
    }
}
