using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Communication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommunicationMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "comms");

            migrationBuilder.CreateTable(
                name: "communication_flow_conversions",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowInstanceRecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoalKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StepOrderAtConversion = table.Column<int>(type: "integer", nullable: false),
                    ConversionValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ReferenceKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_flow_conversions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_flow_instance_recipients",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmailAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentStepIndex = table.Column<int>(type: "integer", nullable: false),
                    CompletedStepsCount = table.Column<int>(type: "integer", nullable: false),
                    OpenedStepsCount = table.Column<int>(type: "integer", nullable: false),
                    ClickedStepsCount = table.Column<int>(type: "integer", nullable: false),
                    EnrolledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastStepSentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextStepDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExitedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExitReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    merge_fields = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_flow_instance_recipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_flow_instances",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortLabel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_flow_instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_flows",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TopicKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TriggerMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExitCondition = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ConversionGoalKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ConversionTargetGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConversionGoalUrlPrefix = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConversionWindowDays = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    steps = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_flows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_recipients",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommunicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedDestination = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TransportKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BounceKind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OpenedCount = table.Column<int>(type: "integer", nullable: false),
                    FirstOpenedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClickedCount = table.Column<int>(type: "integer", nullable: false),
                    FirstClickedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastClickedUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StatusNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    merge_fields = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_recipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_responses",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemPhoneNumber = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ParticipantPhoneNumber = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ParticipantPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParticipantDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Direction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MessageBody = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: false),
                    MatchedKeyword = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    RepliedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_responses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_subscription_preferences",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NormalizedDestination = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    GlobalOptOut = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    opted_out_topics = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_subscription_preferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_templates",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TopicKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PreheaderText = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    FromName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FromEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    ReplyToEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    SenderSystemPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: true),
                    RazorLayoutKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SmsBodyTemplate = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    PushTitleTemplate = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PushBodyTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    sections = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communications",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TopicKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsBulk = table.Column<bool>(type: "boolean", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PreheaderText = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    FromName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FromEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    ReplyToEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    SenderSystemPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: true),
                    RazorLayoutKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    HtmlBodyOverride = table.Column<string>(type: "text", nullable: true),
                    SmsBody = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    PushTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PushBody = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PushOpenUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewerPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ScheduledSendAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    sections = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "comms",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.tenant_id, x.message_id, x.consumer_name });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "comms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "personal_devices",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Platform = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PushRegistrationToken = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DeviceVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sms_pipelines",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SystemPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: true),
                    PriorityOrder = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_pipelines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_communications",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FromEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    SubjectTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HtmlBodyTemplate = table.Column<string>(type: "text", nullable: false),
                    RazorLayoutKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SmsFromSystemPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: true),
                    SmsMessageTemplate = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    PushTitleTemplate = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PushMessageTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PushOpenUrlTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_communications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_notification_logs",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemCommunicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemCommunicationKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TransportKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RecipientAddress = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    RecipientPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectOrTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TriggerSource = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SentOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_notification_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_phone_numbers",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Number = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsSmsForwardingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedToPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    MobileApplicationSiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderIdentifier = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_phone_numbers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "template_snippets",
                schema: "comms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    documentation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    markup = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    output_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    category_ids = table.Column<string>(type: "jsonb", nullable: false),
                    default_parameters = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_snippets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sms_actions",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SmsPipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    MatchMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Keyword = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ActionKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReplyTemplate = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    TargetSystemCommunicationKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContinueProcessing = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sms_actions_sms_pipelines_SmsPipelineId",
                        column: x => x.SmsPipelineId,
                        principalSchema: "comms",
                        principalTable: "sms_pipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_conversions_FlowId",
                schema: "comms",
                table: "communication_flow_conversions",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_conversions_PersonId",
                schema: "comms",
                table: "communication_flow_conversions",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_conversions_tenant_id_FlowInstanceRecipi~",
                schema: "comms",
                table: "communication_flow_conversions",
                columns: new[] { "tenant_id", "FlowInstanceRecipientId", "GoalKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instance_recipients_FlowId_Status",
                schema: "comms",
                table: "communication_flow_instance_recipients",
                columns: new[] { "FlowId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instance_recipients_FlowInstanceId",
                schema: "comms",
                table: "communication_flow_instance_recipients",
                column: "FlowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instance_recipients_PersonId",
                schema: "comms",
                table: "communication_flow_instance_recipients",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instance_recipients_Status_NextStepDueAt~",
                schema: "comms",
                table: "communication_flow_instance_recipients",
                columns: new[] { "Status", "NextStepDueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instance_recipients_tenant_id",
                schema: "comms",
                table: "communication_flow_instance_recipients",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instances_FlowId_Status",
                schema: "comms",
                table: "communication_flow_instances",
                columns: new[] { "FlowId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instances_tenant_id",
                schema: "comms",
                table: "communication_flow_instances",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_communication_flows_Category_IsActive",
                schema: "comms",
                table: "communication_flows",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_flows_tenant_id_Key",
                schema: "comms",
                table: "communication_flows",
                columns: new[] { "tenant_id", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_flows_tenant_id_SystemGuid",
                schema: "comms",
                table: "communication_flows",
                columns: new[] { "tenant_id", "SystemGuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_recipients_CommunicationId_Status",
                schema: "comms",
                table: "communication_recipients",
                columns: new[] { "CommunicationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_recipients_PersonId",
                schema: "comms",
                table: "communication_recipients",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_recipients_ProviderMessageId",
                schema: "comms",
                table: "communication_recipients",
                column: "ProviderMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_recipients_tenant_id_CommunicationId_Normaliz~",
                schema: "comms",
                table: "communication_recipients",
                columns: new[] { "tenant_id", "CommunicationId", "NormalizedDestination" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_responses_IsRead_Direction_OccurredAtUtc",
                schema: "comms",
                table: "communication_responses",
                columns: new[] { "IsRead", "Direction", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_responses_ParticipantPersonId",
                schema: "comms",
                table: "communication_responses",
                column: "ParticipantPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_responses_SystemPhoneNumberId_ParticipantPhon~",
                schema: "comms",
                table: "communication_responses",
                columns: new[] { "SystemPhoneNumberId", "ParticipantPhoneNumber", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_responses_tenant_id",
                schema: "comms",
                table: "communication_responses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_communication_subscription_preferences_PersonId",
                schema: "comms",
                table: "communication_subscription_preferences",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_subscription_preferences_tenant_id_Channel_No~",
                schema: "comms",
                table: "communication_subscription_preferences",
                columns: new[] { "tenant_id", "Channel", "NormalizedDestination" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_templates_Channel_IsActive",
                schema: "comms",
                table: "communication_templates",
                columns: new[] { "Channel", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_templates_tenant_id_Key",
                schema: "comms",
                table: "communication_templates",
                columns: new[] { "tenant_id", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_templates_tenant_id_SystemGuid",
                schema: "comms",
                table: "communication_templates",
                columns: new[] { "tenant_id", "SystemGuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communications_CreatedOnUtc",
                schema: "comms",
                table: "communications",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_communications_Status_ScheduledSendAtUtc",
                schema: "comms",
                table: "communications",
                columns: new[] { "Status", "ScheduledSendAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_communications_tenant_id",
                schema: "comms",
                table: "communications",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "comms",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_personal_devices_PersonId_IsActive_NotificationsEnabled",
                schema: "comms",
                table: "personal_devices",
                columns: new[] { "PersonId", "IsActive", "NotificationsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_personal_devices_tenant_id_PushRegistrationToken",
                schema: "comms",
                table: "personal_devices",
                columns: new[] { "tenant_id", "PushRegistrationToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sms_actions_SmsPipelineId_Order",
                schema: "comms",
                table: "sms_actions",
                columns: new[] { "SmsPipelineId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_actions_tenant_id",
                schema: "comms",
                table: "sms_actions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sms_pipelines_IsActive_PriorityOrder",
                schema: "comms",
                table: "sms_pipelines",
                columns: new[] { "IsActive", "PriorityOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_pipelines_tenant_id_SystemGuid",
                schema: "comms",
                table: "sms_pipelines",
                columns: new[] { "tenant_id", "SystemGuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_communications_Category_IsActive",
                schema: "comms",
                table: "system_communications",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_system_communications_tenant_id_Key",
                schema: "comms",
                table: "system_communications",
                columns: new[] { "tenant_id", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_communications_tenant_id_SystemGuid",
                schema: "comms",
                table: "system_communications",
                columns: new[] { "tenant_id", "SystemGuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_notification_logs_RecipientPersonId",
                schema: "comms",
                table: "system_notification_logs",
                column: "RecipientPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_system_notification_logs_SentOnUtc",
                schema: "comms",
                table: "system_notification_logs",
                column: "SentOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_system_notification_logs_SystemCommunicationKey_SentOnUtc",
                schema: "comms",
                table: "system_notification_logs",
                columns: new[] { "SystemCommunicationKey", "SentOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_system_notification_logs_tenant_id",
                schema: "comms",
                table: "system_notification_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_phone_numbers_AssignedToPersonId",
                schema: "comms",
                table: "system_phone_numbers",
                column: "AssignedToPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_system_phone_numbers_tenant_id_Number",
                schema: "comms",
                table: "system_phone_numbers",
                columns: new[] { "tenant_id", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_phone_numbers_tenant_id_SystemGuid",
                schema: "comms",
                table: "system_phone_numbers",
                columns: new[] { "tenant_id", "SystemGuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_template_snippets_tenant_id_key",
                schema: "comms",
                table: "template_snippets",
                columns: new[] { "tenant_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_template_snippets_tenant_id_system_guid",
                schema: "comms",
                table: "template_snippets",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(migrationBuilder, "comms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "communication_flow_conversions",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_flow_instance_recipients",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_flow_instances",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_flows",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_recipients",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_responses",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_subscription_preferences",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_templates",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communications",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "personal_devices",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "sms_actions",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "system_communications",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "system_notification_logs",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "system_phone_numbers",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "template_snippets",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "sms_pipelines",
                schema: "comms");
        }
    }
}
