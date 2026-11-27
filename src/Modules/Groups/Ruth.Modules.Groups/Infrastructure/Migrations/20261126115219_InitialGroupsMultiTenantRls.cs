using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Ruth.Modules.Groups.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialGroupsMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "groups");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "attendance_occurrences",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootGroupTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurrenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WeekEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OccurrenceDateKey = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DidNotOccur = table.Column<bool>(type: "boolean", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AnonymousAttendanceCount = table.Column<int>(type: "integer", nullable: true),
                    AcceptConfirmationMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DeclineConfirmationMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ShowDeclineReasons = table.Column<bool>(type: "boolean", nullable: false),
                    AttendanceTypeValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_occurrences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_demographic_types",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ComponentKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsAutomated = table.Column<bool>(type: "boolean", nullable: false),
                    RunOnPersonUpdate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_demographic_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_demographic_values",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupDemographicTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ValueAsNumeric = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ValueAsBoolean = table.Column<bool>(type: "boolean", nullable: true),
                    ValueAsGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityTypeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastCalculatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_demographic_values", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_historicals",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GroupTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupTypeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StatusValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    InactiveDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArchivedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArchivedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpireDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentRowIndicator = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_historicals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_location_schedule_configs",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MinimumCapacity = table.Column<int>(type: "integer", nullable: true),
                    DesiredCapacity = table.Column<int>(type: "integer", nullable: true),
                    MaximumCapacity = table.Column<int>(type: "integer", nullable: true),
                    ConfirmationAdditionalDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReminderAdditionalDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_location_schedule_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_member_assignments",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmationSentOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastReminderSentOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_member_assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_member_historicals",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupRoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsLeader = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    ArchivedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArchivedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    InactiveDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectiveDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpireDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentRowIndicator = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_member_historicals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_member_requirements",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupRequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequirementMetOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequirementFailOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequirementWarningOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastCheckedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WasManuallyCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    ManuallyCompletedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManuallyCompletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WasOverridden = table.Column<bool>(type: "boolean", nullable: false),
                    OverriddenByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    OverriddenOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OverrideNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_member_requirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_member_schedule_templates",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GroupTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurrenceDescription = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IntervalWeeks = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_member_schedule_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GuestCount = table.Column<int>(type: "integer", nullable: true),
                    DateTimeAddedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InactiveDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    ArchivedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArchivedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupOrder = table.Column<int>(type: "integer", nullable: true),
                    IsNotified = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduleTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ScheduleReminderEmailOffsetDays = table.Column<int>(type: "integer", nullable: true),
                    CommunicationPreference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    attributes = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_requirement_types",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CheckType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RuleKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RuleValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CanExpire = table.Column<bool>(type: "boolean", nullable: false),
                    ExpireInDays = table.Column<int>(type: "integer", nullable: true),
                    WarningDaysBeforeExpiration = table.Column<int>(type: "integer", nullable: true),
                    PositiveLabel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    NegativeLabel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    WarningLabel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CheckboxLabel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IconCssClass = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DueDateType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DueDateOffsetInDays = table.Column<int>(type: "integer", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_requirement_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_requirements",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupRequirementTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    MustMeetRequirementToAddMember = table.Column<bool>(type: "boolean", nullable: false),
                    AllowLeadersToOverride = table.Column<bool>(type: "boolean", nullable: false),
                    AppliesToAgeClassification = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DueDateStaticDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DueDateAttributeKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_requirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_schedule_exclusions",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_schedule_exclusions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_sync_rules",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupTypeRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceFilterValue = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ScheduleIntervalMinutes = table.Column<int>(type: "integer", nullable: true),
                    AddMissingMembers = table.Column<bool>(type: "boolean", nullable: false),
                    RemoveExtraMembers = table.Column<bool>(type: "boolean", nullable: false),
                    TargetMemberStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastSyncOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAddedCount = table.Column<int>(type: "integer", nullable: false),
                    LastRemovedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_sync_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_types",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GroupTerm = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GroupMemberTerm = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IconCssClass = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    ShowInGroupList = table.Column<bool>(type: "boolean", nullable: false),
                    ShowInNavigation = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMultipleLocations = table.Column<bool>(type: "boolean", nullable: false),
                    IsMeetingStyleEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    GroupsRequireBranch = table.Column<bool>(type: "boolean", nullable: false),
                    EnableGroupHistory = table.Column<bool>(type: "boolean", nullable: false),
                    TakesAttendance = table.Column<bool>(type: "boolean", nullable: false),
                    CountsTowardHeadlineAttendance = table.Column<bool>(type: "boolean", nullable: false),
                    SendAttendanceReminder = table.Column<bool>(type: "boolean", nullable: false),
                    GroupAttendanceRequiresLocation = table.Column<bool>(type: "boolean", nullable: false),
                    GroupAttendanceRequiresSchedule = table.Column<bool>(type: "boolean", nullable: false),
                    AttendanceRule = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    GroupCapacityRule = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LocationSelectionMode = table.Column<int>(type: "integer", nullable: false),
                    EnableLocationSchedules = table.Column<bool>(type: "boolean", nullable: false),
                    IsSchedulingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduleConfirmationLogic = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ScheduleReminderEmailOffsetDays = table.Column<int>(type: "integer", nullable: true),
                    PurposeValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultGroupRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    InheritedGroupTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    attributes = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PhotoId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeetingStyle = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    AllowGuests = table.Column<bool>(type: "boolean", nullable: false),
                    GroupCapacity = table.Column<int>(type: "integer", nullable: true),
                    StatusValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequiredSignatureDocumentTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    InactiveDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArchivedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArchivedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    SchedulingMustMeetRequirements = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduleCoordinatorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupAdministratorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisableScheduling = table.Column<bool>(type: "boolean", nullable: false),
                    DisableScheduleToolboxAccess = table.Column<bool>(type: "boolean", nullable: false),
                    RsvpReminderOffsetDays = table.Column<int>(type: "integer", nullable: true),
                    attributes = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "groups",
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
                schema: "groups",
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
                name: "person_schedule_exclusions",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentExclusionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_schedule_exclusions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "attendances",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DidAttend = table.Column<bool>(type: "boolean", nullable: true),
                    IsFirstTime = table.Column<bool>(type: "boolean", nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Rsvp = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RsvpDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RsvpToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeclineReasonValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeclineReasonNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequestedToAttend = table.Column<bool>(type: "boolean", nullable: true),
                    ScheduledToAttend = table.Column<bool>(type: "boolean", nullable: true),
                    ScheduledByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleConfirmationSent = table.Column<bool>(type: "boolean", nullable: true),
                    ScheduleReminderSent = table.Column<bool>(type: "boolean", nullable: true),
                    CheckedInByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendances_attendance_occurrences_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalSchema: "groups",
                        principalTable: "attendance_occurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_type_allowed_children",
                schema: "groups",
                columns: table => new
                {
                    ParentGroupTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildGroupTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_type_allowed_children", x => new { x.ParentGroupTypeId, x.ChildGroupTypeId });
                    table.ForeignKey(
                        name: "FK_group_type_allowed_children_group_types_ParentGroupTypeId",
                        column: x => x.ParentGroupTypeId,
                        principalSchema: "groups",
                        principalTable: "group_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_type_roles",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    MinCount = table.Column<int>(type: "integer", nullable: true),
                    MaxCount = table.Column<int>(type: "integer", nullable: true),
                    IsLeader = table.Column<bool>(type: "boolean", nullable: false),
                    ReceiveRequirementsNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: false),
                    CanEdit = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageMembers = table.Column<bool>(type: "boolean", nullable: false),
                    CanTakeAttendance = table.Column<bool>(type: "boolean", nullable: false),
                    IsCheckInAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_type_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_type_roles_group_types_GroupTypeId",
                        column: x => x.GroupTypeId,
                        principalSchema: "groups",
                        principalTable: "group_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_locations",
                schema: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    GroupLocationTypeValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsMailingLocation = table.Column<bool>(type: "boolean", nullable: false),
                    IsMappedLocation = table.Column<bool>(type: "boolean", nullable: false),
                    IsOverflowLocation = table.Column<bool>(type: "boolean", nullable: false),
                    GroupMemberPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    geo_point = table.Column<Point>(type: "geography(Point, 4326)", nullable: true),
                    geo_fence = table.Column<Polygon>(type: "geography(Polygon, 4326)", nullable: true),
                    schedule_ids = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_locations_groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "groups",
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_occurrences_OccurrenceDate",
                schema: "groups",
                table: "attendance_occurrences",
                column: "OccurrenceDate");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_occurrences_OccurrenceDateKey",
                schema: "groups",
                table: "attendance_occurrences",
                column: "OccurrenceDateKey");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_occurrences_tenant_id_GroupId_LocationId_Schedul~",
                schema: "groups",
                table: "attendance_occurrences",
                columns: new[] { "tenant_id", "GroupId", "LocationId", "ScheduleId", "OccurrenceDate" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_occurrences_WeekEndDate",
                schema: "groups",
                table: "attendance_occurrences",
                column: "WeekEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_BranchId",
                schema: "groups",
                table: "attendances",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_OccurrenceId",
                schema: "groups",
                table: "attendances",
                column: "OccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_PersonId",
                schema: "groups",
                table: "attendances",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_StartDateTimeUtc",
                schema: "groups",
                table: "attendances",
                column: "StartDateTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_tenant_id_RsvpToken",
                schema: "groups",
                table: "attendances",
                columns: new[] { "tenant_id", "RsvpToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_demographic_types_tenant_id_GroupTypeId_Name",
                schema: "groups",
                table: "group_demographic_types",
                columns: new[] { "tenant_id", "GroupTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_demographic_values_tenant_id_GroupId_GroupDemographic~",
                schema: "groups",
                table: "group_demographic_values",
                columns: new[] { "tenant_id", "GroupId", "GroupDemographicTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_historicals_GroupId_CurrentRowIndicator",
                schema: "groups",
                table: "group_historicals",
                columns: new[] { "GroupId", "CurrentRowIndicator" });

            migrationBuilder.CreateIndex(
                name: "IX_group_historicals_GroupId_EffectiveDateTimeUtc",
                schema: "groups",
                table: "group_historicals",
                columns: new[] { "GroupId", "EffectiveDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_group_historicals_tenant_id",
                schema: "groups",
                table: "group_historicals",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_location_schedule_configs_tenant_id_GroupLocationId_S~",
                schema: "groups",
                table: "group_location_schedule_configs",
                columns: new[] { "tenant_id", "GroupLocationId", "ScheduleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_locations_geo_fence",
                schema: "groups",
                table: "group_locations",
                column: "geo_fence")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_group_locations_geo_point",
                schema: "groups",
                table: "group_locations",
                column: "geo_point")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_group_locations_GroupId",
                schema: "groups",
                table: "group_locations",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_locations_LocationId",
                schema: "groups",
                table: "group_locations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_group_locations_tenant_id_GroupId_LocationId",
                schema: "groups",
                table: "group_locations",
                columns: new[] { "tenant_id", "GroupId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_member_assignments_GroupId",
                schema: "groups",
                table: "group_member_assignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_member_assignments_tenant_id_GroupMemberId_LocationId~",
                schema: "groups",
                table: "group_member_assignments",
                columns: new[] { "tenant_id", "GroupMemberId", "LocationId", "ScheduleId" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_group_member_historicals_GroupMemberId_CurrentRowIndicator",
                schema: "groups",
                table: "group_member_historicals",
                columns: new[] { "GroupMemberId", "CurrentRowIndicator" });

            migrationBuilder.CreateIndex(
                name: "IX_group_member_historicals_PersonId",
                schema: "groups",
                table: "group_member_historicals",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_group_member_historicals_tenant_id",
                schema: "groups",
                table: "group_member_historicals",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_member_requirements_tenant_id_GroupMemberId_GroupRequ~",
                schema: "groups",
                table: "group_member_requirements",
                columns: new[] { "tenant_id", "GroupMemberId", "GroupRequirementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_member_schedule_templates_GroupTypeId",
                schema: "groups",
                table: "group_member_schedule_templates",
                column: "GroupTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_group_member_schedule_templates_tenant_id",
                schema: "groups",
                table: "group_member_schedule_templates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_members_attributes",
                schema: "groups",
                table: "group_members",
                column: "attributes")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_group_members_GroupId_Status_IsArchived",
                schema: "groups",
                table: "group_members",
                columns: new[] { "GroupId", "Status", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_group_members_PersonId",
                schema: "groups",
                table: "group_members",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_group_members_tenant_id_GroupId_PersonId_GroupRoleId",
                schema: "groups",
                table: "group_members",
                columns: new[] { "tenant_id", "GroupId", "PersonId", "GroupRoleId" },
                unique: true,
                filter: "\"IsArchived\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_group_requirement_types_tenant_id_Name",
                schema: "groups",
                table: "group_requirement_types",
                columns: new[] { "tenant_id", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_requirements_GroupId",
                schema: "groups",
                table: "group_requirements",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_requirements_GroupTypeId",
                schema: "groups",
                table: "group_requirements",
                column: "GroupTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_group_requirements_tenant_id_GroupId_GroupTypeId_GroupRequi~",
                schema: "groups",
                table: "group_requirements",
                columns: new[] { "tenant_id", "GroupId", "GroupTypeId", "GroupRequirementTypeId", "GroupRoleId" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_group_schedule_exclusions_GroupTypeId_StartDate_EndDate",
                schema: "groups",
                table: "group_schedule_exclusions",
                columns: new[] { "GroupTypeId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_group_schedule_exclusions_tenant_id",
                schema: "groups",
                table: "group_schedule_exclusions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_sync_rules_tenant_id_GroupId_GroupTypeRoleId",
                schema: "groups",
                table: "group_sync_rules",
                columns: new[] { "tenant_id", "GroupId", "GroupTypeRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_type_allowed_children_ChildGroupTypeId",
                schema: "groups",
                table: "group_type_allowed_children",
                column: "ChildGroupTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_group_type_allowed_children_tenant_id",
                schema: "groups",
                table: "group_type_allowed_children",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_type_roles_GroupTypeId",
                schema: "groups",
                table: "group_type_roles",
                column: "GroupTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_group_type_roles_tenant_id_GroupTypeId_Name",
                schema: "groups",
                table: "group_type_roles",
                columns: new[] { "tenant_id", "GroupTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_types_attributes",
                schema: "groups",
                table: "group_types",
                column: "attributes")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_group_types_Order",
                schema: "groups",
                table: "group_types",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_group_types_tenant_id_Name",
                schema: "groups",
                table: "group_types",
                columns: new[] { "tenant_id", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_groups_attributes",
                schema: "groups",
                table: "groups",
                column: "attributes")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_groups_BranchId",
                schema: "groups",
                table: "groups",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_groups_GroupTypeId",
                schema: "groups",
                table: "groups",
                column: "GroupTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_groups_IsActive_IsArchived_IsPublic",
                schema: "groups",
                table: "groups",
                columns: new[] { "IsActive", "IsArchived", "IsPublic" });

            migrationBuilder.CreateIndex(
                name: "IX_groups_ParentGroupId",
                schema: "groups",
                table: "groups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_groups_tenant_id",
                schema: "groups",
                table: "groups",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "groups",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_person_schedule_exclusions_GroupId",
                schema: "groups",
                table: "person_schedule_exclusions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_person_schedule_exclusions_PersonId_StartDate_EndDate",
                schema: "groups",
                table: "person_schedule_exclusions",
                columns: new[] { "PersonId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_person_schedule_exclusions_tenant_id",
                schema: "groups",
                table: "person_schedule_exclusions",
                column: "tenant_id");
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(migrationBuilder, "groups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendances",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_demographic_types",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_demographic_values",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_historicals",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_location_schedule_configs",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_locations",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_member_assignments",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_member_historicals",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_member_requirements",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_member_schedule_templates",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_members",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_requirement_types",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_requirements",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_schedule_exclusions",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_sync_rules",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_type_allowed_children",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_type_roles",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "person_schedule_exclusions",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "attendance_occurrences",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "groups",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "group_types",
                schema: "groups");
        }
    }
}
