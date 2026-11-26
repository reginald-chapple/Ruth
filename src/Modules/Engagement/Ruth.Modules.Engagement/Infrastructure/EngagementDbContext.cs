using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Domain.Achievements;
using Ruth.Modules.Engagement.Domain.Following;
using Ruth.Modules.Engagement.Domain.Journeys;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Modules.Engagement.Domain.Reminders;
using Ruth.Modules.Engagement.Domain.Streaks;
using Ruth.Modules.Engagement.Domain.SupportRequests;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Engagement.Infrastructure;

/// <summary>EF Core database context exclusively owning the engage PostgreSQL schema.</summary>
public sealed class EngagementDbContext(DbContextOptions<EngagementDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>PostgreSQL schema name owned by the Engagement module.</summary>
    public const string SchemaName = "engage";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    public DbSet<PipelineType> PipelineTypes => Set<PipelineType>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<PipelineActivityType> PipelineActivityTypes => Set<PipelineActivityType>();
    public DbSet<EngagementOpportunity> EngagementOpportunities => Set<EngagementOpportunity>();
    public DbSet<EngagementRequest> EngagementRequests => Set<EngagementRequest>();
    public DbSet<EngagementRequestActivity> EngagementRequestActivities =>
        Set<EngagementRequestActivity>();

    public DbSet<JourneyProgram> JourneyPrograms => Set<JourneyProgram>();
    public DbSet<MilestoneStatus> MilestoneStatuses => Set<MilestoneStatus>();
    public DbSet<MilestoneType> MilestoneTypes => Set<MilestoneType>();
    public DbSet<PersonMilestone> PersonMilestones => Set<PersonMilestone>();
    public DbSet<JourneyCompletion> JourneyCompletions => Set<JourneyCompletion>();

    public DbSet<StreakType> StreakTypes => Set<StreakType>();
    public DbSet<StreakExclusion> StreakExclusions => Set<StreakExclusion>();
    public DbSet<Streak> Streaks => Set<Streak>();

    public DbSet<AchievementType> AchievementTypes => Set<AchievementType>();
    public DbSet<AchievementAttempt> AchievementAttempts => Set<AchievementAttempt>();

    public DbSet<SupportCategory> SupportCategories => Set<SupportCategory>();
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<SupportRequestComment> SupportRequestComments => Set<SupportRequestComment>();

    public DbSet<ReminderType> ReminderTypes => Set<ReminderType>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();

    public DbSet<FollowingSubscription> FollowingSubscriptions => Set<FollowingSubscription>();
    public DbSet<FollowingEventNotification> FollowingEventNotifications =>
        Set<FollowingEventNotification>();

    public DbSet<ConstituentContact> ConstituentContacts => Set<ConstituentContact>();
    public DbSet<Touchpoint> Touchpoints => Set<Touchpoint>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.EngagementModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
