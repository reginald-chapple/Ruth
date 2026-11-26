using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Engagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialEngagementW5S1ToS4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "engage");

            migrationBuilder.CreateTable(
                name: "achievement_attempts",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AttemptEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CurrentCount = table.Column<int>(type: "integer", nullable: false),
                    TargetCountSnapshot = table.Column<int>(type: "integer", nullable: false),
                    Progress = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_attempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "achievement_types",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetCount = table.Column<int>(type: "integer", nullable: false),
                    TargetStreakTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetJourneyProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetMilestoneTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowOverAchievement = table.Column<bool>(type: "boolean", nullable: false),
                    MaxSuccessesAllowed = table.Column<int>(type: "integer", nullable: true),
                    BadgeIconCssClass = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HighlightColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    CelebrationSystemCommunicationKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    prerequisite_achievement_ids = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "constituent_contacts",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstituentPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    PortfolioRole = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RelationshipStrength = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    OutreachCadenceDays = table.Column<int>(type: "integer", nullable: false),
                    ConnectionNoteCadenceDays = table.Column<int>(type: "integer", nullable: true),
                    PreferredChannel = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    LastTouchpointAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastConnectionNoteAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LatestSentiment = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    NextScheduledTouchpointDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HealthScore = table.Column<int>(type: "integer", nullable: false),
                    HealthStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_constituent_contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "engagement_opportunities",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    PublicSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IconCssClass = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhotoBinaryFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultBranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultConnectorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    branch_configs = table.Column<string>(type: "jsonb", nullable: false),
                    placement_group_configs = table.Column<string>(type: "jsonb", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_opportunities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "engagement_requests",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EngagementOpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedConnectorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlacementGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlacementGroupRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestState = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    FollowUpDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastActivityAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConnectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "following_event_notifications",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityTypeKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventTypeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DigestProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_following_event_notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "following_subscriptions",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowerPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityTypeKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityDisplayName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    PurposeKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsSubscribed = table.Column<bool>(type: "boolean", nullable: false),
                    LastNotifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_following_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "engage",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.message_id, x.consumer_name });
                });

            migrationBuilder.CreateTable(
                name: "journey_completions",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    JourneyProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journey_completions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "journey_programs",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultListView = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CompletionMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journey_programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_messages",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "engage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_retry_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    locked_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lock_token = table.Column<Guid>(type: "uuid", nullable: true),
                    error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "person_milestones",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    JourneyProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    MilestoneTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MilestoneStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_milestones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pipeline_types",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EnableFutureFollowUp = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultFollowUpDays = table.Column<int>(type: "integer", nullable: false),
                    DaysUntilRequestIdle = table.Column<int>(type: "integer", nullable: false),
                    RequiresPlacementGroup = table.Column<bool>(type: "boolean", nullable: false),
                    IsSequentialStageProgression = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipeline_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reminder_types",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EntityTypeKeyFilter = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    HighlightColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ShouldAutoCompleteWhenNotified = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationFlavor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SystemCommunicationKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reminder_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reminders",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReminderTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssigneePersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetEntityTypeKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntitySummary = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReminderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RenewPeriodDays = table.Column<int>(type: "integer", nullable: true),
                    RenewMaxCount = table.Column<int>(type: "integer", nullable: true),
                    RenewCurrentCount = table.Column<int>(type: "integer", nullable: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastNotifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reminders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "streak_types",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Cadence = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    WeekEndDay = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AnchorStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequiresExplicitEnrollment = table.Column<bool>(type: "boolean", nullable: false),
                    EnableAchievements = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streak_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "streaks",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StreakTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnrollmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InactiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    engagement_map = table.Column<byte[]>(type: "bytea", nullable: false),
                    EngagementCount = table.Column<int>(type: "integer", nullable: false),
                    CurrentStreakCount = table.Column<int>(type: "integer", nullable: false),
                    CurrentStreakStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LongestStreakCount = table.Column<int>(type: "integer", nullable: false),
                    LongestStreakStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LongestStreakEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MostRecentEngagementDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streaks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "support_categories",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequiresModeration = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultExpirationDays = table.Column<int>(type: "integer", nullable: false),
                    AllowAnonymousIntake = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPublicCommunityWall = table.Column<bool>(type: "boolean", nullable: false),
                    AllowComments = table.Column<bool>(type: "boolean", nullable: false),
                    AutoFlagHoldThreshold = table.Column<int>(type: "integer", nullable: false),
                    DefaultCoordinatorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "support_requests",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupportCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedCareCoordinatorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AnswerOrOutcomeSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VisibilityLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ModerationState = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsUrgent = table.Column<bool>(type: "boolean", nullable: false),
                    AllowComments = table.Column<bool>(type: "boolean", nullable: false),
                    CommunitySupportCount = table.Column<int>(type: "integer", nullable: false),
                    FlagCount = table.Column<int>(type: "integer", nullable: false),
                    EnteredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "touchpoints",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstituentContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstituentPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Channel = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OutcomeSentiment = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SnoozeCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_touchpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "engagement_request_activities",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngagementRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ConnectorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagement_request_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_engagement_request_activities_engagement_requests_Engagemen~",
                        column: x => x.EngagementRequestId,
                        principalSchema: "engage",
                        principalTable: "engagement_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "milestone_statuses",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JourneyProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsCompleteStatus = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_statuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_milestone_statuses_journey_programs_JourneyProgramId",
                        column: x => x.JourneyProgramId,
                        principalSchema: "engage",
                        principalTable: "journey_programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "milestone_types",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JourneyProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IconCssClass = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsRequiredForProgramCompletion = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMultipleCompletions = table.Column<bool>(type: "boolean", nullable: false),
                    HasEndDate = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    prerequisite_milestone_type_ids = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_milestone_types_journey_programs_JourneyProgramId",
                        column: x => x.JourneyProgramId,
                        principalSchema: "engage",
                        principalTable: "journey_programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pipeline_activity_types",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipeline_activity_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pipeline_activity_types_pipeline_types_PipelineTypeId",
                        column: x => x.PipelineTypeId,
                        principalSchema: "engage",
                        principalTable: "pipeline_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pipeline_stages",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsInitial = table.Column<bool>(type: "boolean", nullable: false),
                    IsTerminalCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsTerminalInactive = table.Column<bool>(type: "boolean", nullable: false),
                    IdleThresholdDaysOverride = table.Column<int>(type: "integer", nullable: true),
                    automations = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipeline_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pipeline_stages_pipeline_types_PipelineTypeId",
                        column: x => x.PipelineTypeId,
                        principalSchema: "engage",
                        principalTable: "pipeline_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "streak_exclusions",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StreakTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExclusionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streak_exclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_streak_exclusions_streak_types_StreakTypeId",
                        column: x => x.StreakTypeId,
                        principalSchema: "engage",
                        principalTable: "streak_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "support_request_comments",
                schema: "engage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupportRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsInternalCareNote = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_request_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_support_request_comments_support_requests_SupportRequestId",
                        column: x => x.SupportRequestId,
                        principalSchema: "engage",
                        principalTable: "support_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_achievement_attempts_PersonId_AchievementTypeId",
                schema: "engage",
                table: "achievement_attempts",
                columns: new[] { "PersonId", "AchievementTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_achievement_types_Code",
                schema: "engage",
                table: "achievement_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_constituent_contacts_attribute_values",
                schema: "engage",
                table: "constituent_contacts",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_constituent_contacts_OwnerPersonId_ConstituentPersonId",
                schema: "engage",
                table: "constituent_contacts",
                columns: new[] { "OwnerPersonId", "ConstituentPersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_constituent_contacts_OwnerPersonId_IsActive_HealthScore",
                schema: "engage",
                table: "constituent_contacts",
                columns: new[] { "OwnerPersonId", "IsActive", "HealthScore" });

            migrationBuilder.CreateIndex(
                name: "IX_engagement_opportunities_attribute_values",
                schema: "engage",
                table: "engagement_opportunities",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_engagement_opportunities_PipelineTypeId_IsActive_IsPublic",
                schema: "engage",
                table: "engagement_opportunities",
                columns: new[] { "PipelineTypeId", "IsActive", "IsPublic" });

            migrationBuilder.CreateIndex(
                name: "IX_engagement_opportunities_Slug",
                schema: "engage",
                table: "engagement_opportunities",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_engagement_request_activities_EngagementRequestId_OccurredA~",
                schema: "engage",
                table: "engagement_request_activities",
                columns: new[] { "EngagementRequestId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_engagement_requests_attribute_values",
                schema: "engage",
                table: "engagement_requests",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_engagement_requests_PipelineTypeId_RequestState",
                schema: "engage",
                table: "engagement_requests",
                columns: new[] { "PipelineTypeId", "RequestState" });

            migrationBuilder.CreateIndex(
                name: "IX_engagement_requests_RequesterPersonId",
                schema: "engage",
                table: "engagement_requests",
                column: "RequesterPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_engagement_requests_RequestState_FollowUpDate",
                schema: "engage",
                table: "engagement_requests",
                columns: new[] { "RequestState", "FollowUpDate" });

            migrationBuilder.CreateIndex(
                name: "IX_following_event_notifications_DigestProcessedAtUtc_Occurred~",
                schema: "engage",
                table: "following_event_notifications",
                columns: new[] { "DigestProcessedAtUtc", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_following_subscriptions_FollowerPersonId_EntityTypeKey_Enti~",
                schema: "engage",
                table: "following_subscriptions",
                columns: new[] { "FollowerPersonId", "EntityTypeKey", "EntityId", "PurposeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journey_completions_PersonId_JourneyProgramId",
                schema: "engage",
                table: "journey_completions",
                columns: new[] { "PersonId", "JourneyProgramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journey_programs_attribute_values",
                schema: "engage",
                table: "journey_programs",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_journey_programs_Slug",
                schema: "engage",
                table: "journey_programs",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_milestone_statuses_JourneyProgramId_Order",
                schema: "engage",
                table: "milestone_statuses",
                columns: new[] { "JourneyProgramId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_types_JourneyProgramId_Code",
                schema: "engage",
                table: "milestone_types",
                columns: new[] { "JourneyProgramId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_RecipientPersonId_IsRead_CreatedOnUtc",
                schema: "engage",
                table: "notification_messages",
                columns: new[] { "RecipientPersonId", "IsRead", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "engage",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_person_milestones_MilestoneTypeId_IsCompleted",
                schema: "engage",
                table: "person_milestones",
                columns: new[] { "MilestoneTypeId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_person_milestones_PersonId_JourneyProgramId",
                schema: "engage",
                table: "person_milestones",
                columns: new[] { "PersonId", "JourneyProgramId" });

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_activity_types_PipelineTypeId_Order",
                schema: "engage",
                table: "pipeline_activity_types",
                columns: new[] { "PipelineTypeId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_stages_PipelineTypeId_Order",
                schema: "engage",
                table: "pipeline_stages",
                columns: new[] { "PipelineTypeId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_types_attribute_values",
                schema: "engage",
                table: "pipeline_types",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_pipeline_types_Slug",
                schema: "engage",
                table: "pipeline_types",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reminders_AssigneePersonId",
                schema: "engage",
                table: "reminders",
                column: "AssigneePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_reminders_IsComplete_ReminderDate",
                schema: "engage",
                table: "reminders",
                columns: new[] { "IsComplete", "ReminderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_streak_exclusions_StreakTypeId_ExclusionDate",
                schema: "engage",
                table: "streak_exclusions",
                columns: new[] { "StreakTypeId", "ExclusionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_streak_types_Code",
                schema: "engage",
                table: "streak_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_streaks_PersonId",
                schema: "engage",
                table: "streaks",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_streaks_StreakTypeId_PersonId",
                schema: "engage",
                table: "streaks",
                columns: new[] { "StreakTypeId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_support_categories_Slug",
                schema: "engage",
                table: "support_categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_support_request_comments_SupportRequestId_CreatedAtUtc",
                schema: "engage",
                table: "support_request_comments",
                columns: new[] { "SupportRequestId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_support_requests_attribute_values",
                schema: "engage",
                table: "support_requests",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_support_requests_ModerationState_VisibilityLevel_ExpiresAtU~",
                schema: "engage",
                table: "support_requests",
                columns: new[] { "ModerationState", "VisibilityLevel", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_support_requests_SupportCategoryId",
                schema: "engage",
                table: "support_requests",
                column: "SupportCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_touchpoints_ConstituentContactId",
                schema: "engage",
                table: "touchpoints",
                column: "ConstituentContactId");

            migrationBuilder.CreateIndex(
                name: "IX_touchpoints_OwnerPersonId_Status_ScheduledDate",
                schema: "engage",
                table: "touchpoints",
                columns: new[] { "OwnerPersonId", "Status", "ScheduledDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "achievement_attempts",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "achievement_types",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "constituent_contacts",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "engagement_opportunities",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "engagement_request_activities",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "following_event_notifications",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "following_subscriptions",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "journey_completions",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "milestone_statuses",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "milestone_types",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "notification_messages",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "person_milestones",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "pipeline_activity_types",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "pipeline_stages",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "reminder_types",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "reminders",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "streak_exclusions",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "streaks",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "support_categories",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "support_request_comments",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "touchpoints",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "engagement_requests",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "journey_programs",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "pipeline_types",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "streak_types",
                schema: "engage");

            migrationBuilder.DropTable(
                name: "support_requests",
                schema: "engage");
        }
    }
}
