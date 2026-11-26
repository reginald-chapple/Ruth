using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCrmMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crm");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "assessment_types",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AssessmentPath = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    AssessmentResultsPath = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresRequest = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumDaysToRetake = table.Column<int>(type: "integer", nullable: false),
                    ValidDurationDays = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IconName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BadgeColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessment_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "assessments",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequestedDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CompletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResultDataJson = table.Column<string>(type: "jsonb", nullable: true),
                    SummaryText = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    LastReminderOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "background_checks",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PackageName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResponseId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResponseDocumentBinaryFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RawProviderStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RecordFound = table.Column<bool>(type: "boolean", nullable: true),
                    Recommendation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResponseDataJson = table.Column<string>(type: "jsonb", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_background_checks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "badge_definitions",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BadgeComponentKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetEntityTypeKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    settings = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badge_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "households",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CombineContributionsByDefault = table.Column<bool>(type: "boolean", nullable: false),
                    attributes = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_households", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "identity_verification_codes",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    LastIssuedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_verification_codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "identity_verifications",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IssuedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    VerificationCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FailedMatchAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identity_verifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "crm",
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
                name: "known_relationships",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelatedPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InverseRelationshipId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_known_relationships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "meta_first_name_gender_lookups",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedFirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaleCount = table.Column<int>(type: "integer", nullable: false),
                    FemaleCount = table.Column<int>(type: "integer", nullable: false),
                    TotalCount = table.Column<int>(type: "integer", nullable: false),
                    MalePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    FemalePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Country = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meta_first_name_gender_lookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "meta_nick_name_lookups",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedFirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NickName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedNickName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GenderCode = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meta_nick_name_lookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "metaphones",
                schema: "crm",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrimaryKey = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    AlternateKey = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metaphones", x => new { x.tenant_id, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "crm",
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
                name: "peer_network_edges",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClassificationEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Strength = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RelationshipStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RelationshipEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RelationshipScore = table.Column<decimal>(type: "numeric(8,1)", precision: 8, scale: 1, nullable: false),
                    PreviousScore = table.Column<decimal>(type: "numeric(8,1)", precision: 8, scale: 1, nullable: true),
                    Trend = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Caption = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastUpdatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peer_network_edges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "people",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecordStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecordStatusReasonValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordStatusLastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConnectionStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ConnectionStatusValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountProtectionProfile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedFirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NickName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedNickName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedLastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Suffix = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MaritalStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AnniversaryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BirthYear = table.Column<int>(type: "integer", nullable: true),
                    BirthMonth = table.Column<int>(type: "integer", nullable: true),
                    BirthDay = table.Column<int>(type: "integer", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    GraduationYear = table.Column<int>(type: "integer", nullable: true),
                    IsDeceased = table.Column<bool>(type: "boolean", nullable: false),
                    DeceasedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsLockedAsChild = table.Column<bool>(type: "boolean", nullable: false),
                    AgeClassification = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Email = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: true),
                    IsEmailActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmailNote = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    EmailPreference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CommunicationPreference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PhotoBinaryFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryBranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryHouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    BillingHouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    TopSignalId = table.Column<Guid>(type: "uuid", nullable: true),
                    TopSignalColor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TopSignalIcon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ViewedCount = table.Column<int>(type: "integer", nullable: false),
                    IsMerged = table.Column<bool>(type: "boolean", nullable: false),
                    MergedIntoPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    attributes = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_people", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "person_duplicates",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Person1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Person2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    TotalCapacity = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ScoreDetail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsConfirmedAsNotDuplicate = table.Column<bool>(type: "boolean", nullable: false),
                    IgnoreUntilScoreChanges = table.Column<bool>(type: "boolean", nullable: false),
                    LastScannedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_duplicates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "person_redirects",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    MergeBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalRecordGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    MergedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MergedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_redirects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "person_signals",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExpirationDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_signals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "person_viewed",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewerPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ViewerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ViewedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_viewed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "signal_types",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SignalColor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SignalIcon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signal_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "household_locations",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsMailingLocation = table.Column<bool>(type: "boolean", nullable: false),
                    IsMappedLocation = table.Column<bool>(type: "boolean", nullable: false),
                    AddressSummary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_household_locations_households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "crm",
                        principalTable: "households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "household_members",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    HouseholdOrder = table.Column<int>(type: "integer", nullable: false),
                    IsCombinedContributionMember = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrimaryContact = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_household_members_households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "crm",
                        principalTable: "households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "person_previous_names",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedLastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_previous_names", x => x.Id);
                    table.ForeignKey(
                        name: "FK_person_previous_names_people_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "crm",
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "person_search_keys",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SearchTypeValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    SearchValue = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NormalizedSearchValue = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsValuePrivate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_search_keys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_person_search_keys_people_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "crm",
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phone_numbers",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumberType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NumberTypeValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    E164Number = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    NumberFormatted = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NumberReversed = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Extension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsMessagingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsMessagingOptedOut = table.Column<bool>(type: "boolean", nullable: false),
                    MessagingOptedOutOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsUnlisted = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone_numbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_phone_numbers_people_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "crm",
                        principalTable: "people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assessment_types_tenant_id_NormalizedKey",
                schema: "crm",
                table: "assessment_types",
                columns: new[] { "tenant_id", "NormalizedKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assessments_PersonId_AssessmentTypeId",
                schema: "crm",
                table: "assessments",
                columns: new[] { "PersonId", "AssessmentTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_assessments_Status",
                schema: "crm",
                table: "assessments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_assessments_tenant_id",
                schema: "crm",
                table: "assessments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_background_checks_PersonId",
                schema: "crm",
                table: "background_checks",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_background_checks_ProviderKey_RequestId",
                schema: "crm",
                table: "background_checks",
                columns: new[] { "ProviderKey", "RequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_background_checks_tenant_id",
                schema: "crm",
                table: "background_checks",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_badge_definitions_IsActive_Order",
                schema: "crm",
                table: "badge_definitions",
                columns: new[] { "IsActive", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_badge_definitions_tenant_id",
                schema: "crm",
                table: "badge_definitions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_household_locations_HouseholdId",
                schema: "crm",
                table: "household_locations",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_household_locations_LocationId",
                schema: "crm",
                table: "household_locations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_household_locations_tenant_id_HouseholdId_LocationId",
                schema: "crm",
                table: "household_locations",
                columns: new[] { "tenant_id", "HouseholdId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_household_members_HouseholdId",
                schema: "crm",
                table: "household_members",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_household_members_PersonId_HouseholdOrder",
                schema: "crm",
                table: "household_members",
                columns: new[] { "PersonId", "HouseholdOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_household_members_tenant_id_HouseholdId_PersonId",
                schema: "crm",
                table: "household_members",
                columns: new[] { "tenant_id", "HouseholdId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_households_attributes",
                schema: "crm",
                table: "households",
                column: "attributes")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_households_BranchId",
                schema: "crm",
                table: "households",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_households_IsArchived",
                schema: "crm",
                table: "households",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_households_tenant_id",
                schema: "crm",
                table: "households",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_identity_verification_codes_tenant_id_Code",
                schema: "crm",
                table: "identity_verification_codes",
                columns: new[] { "tenant_id", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_identity_verifications_RequestIpAddress_IssuedOnUtc",
                schema: "crm",
                table: "identity_verifications",
                columns: new[] { "RequestIpAddress", "IssuedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_identity_verifications_tenant_id",
                schema: "crm",
                table: "identity_verifications",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_identity_verifications_VerificationCodeId",
                schema: "crm",
                table: "identity_verifications",
                column: "VerificationCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_known_relationships_InverseRelationshipId",
                schema: "crm",
                table: "known_relationships",
                column: "InverseRelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_known_relationships_RelatedPersonId",
                schema: "crm",
                table: "known_relationships",
                column: "RelatedPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_known_relationships_tenant_id_OwnerPersonId_RelatedPersonId~",
                schema: "crm",
                table: "known_relationships",
                columns: new[] { "tenant_id", "OwnerPersonId", "RelatedPersonId", "RelationshipType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meta_first_name_gender_lookups_NormalizedFirstName",
                schema: "crm",
                table: "meta_first_name_gender_lookups",
                column: "NormalizedFirstName");

            migrationBuilder.CreateIndex(
                name: "IX_meta_first_name_gender_lookups_tenant_id",
                schema: "crm",
                table: "meta_first_name_gender_lookups",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_meta_nick_name_lookups_NormalizedFirstName",
                schema: "crm",
                table: "meta_nick_name_lookups",
                column: "NormalizedFirstName");

            migrationBuilder.CreateIndex(
                name: "IX_meta_nick_name_lookups_NormalizedNickName",
                schema: "crm",
                table: "meta_nick_name_lookups",
                column: "NormalizedNickName");

            migrationBuilder.CreateIndex(
                name: "IX_meta_nick_name_lookups_tenant_id",
                schema: "crm",
                table: "meta_nick_name_lookups",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_metaphones_tenant_id_AlternateKey",
                schema: "crm",
                table: "metaphones",
                columns: new[] { "tenant_id", "AlternateKey" });

            migrationBuilder.CreateIndex(
                name: "IX_metaphones_tenant_id_PrimaryKey",
                schema: "crm",
                table: "metaphones",
                columns: new[] { "tenant_id", "PrimaryKey" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "crm",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_peer_network_edges_SourcePersonId_TargetPersonId_Relationsh~",
                schema: "crm",
                table: "peer_network_edges",
                columns: new[] { "SourcePersonId", "TargetPersonId", "RelationshipType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_peer_network_edges_TargetPersonId",
                schema: "crm",
                table: "peer_network_edges",
                column: "TargetPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_peer_network_edges_tenant_id",
                schema: "crm",
                table: "peer_network_edges",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_people_attributes",
                schema: "crm",
                table: "people",
                column: "attributes")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_people_Email",
                schema: "crm",
                table: "people",
                column: "Email")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_people_FirstName",
                schema: "crm",
                table: "people",
                column: "FirstName")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_people_LastName",
                schema: "crm",
                table: "people",
                column: "LastName")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_people_LastName_FirstName",
                schema: "crm",
                table: "people",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_people_NickName",
                schema: "crm",
                table: "people",
                column: "NickName")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_people_NormalizedEmail",
                schema: "crm",
                table: "people",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_people_NormalizedFirstName",
                schema: "crm",
                table: "people",
                column: "NormalizedFirstName");

            migrationBuilder.CreateIndex(
                name: "IX_people_NormalizedLastName",
                schema: "crm",
                table: "people",
                column: "NormalizedLastName");

            migrationBuilder.CreateIndex(
                name: "IX_people_NormalizedNickName",
                schema: "crm",
                table: "people",
                column: "NormalizedNickName");

            migrationBuilder.CreateIndex(
                name: "IX_people_PrimaryBranchId",
                schema: "crm",
                table: "people",
                column: "PrimaryBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_people_RecordStatus",
                schema: "crm",
                table: "people",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_people_tenant_id",
                schema: "crm",
                table: "people",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_person_duplicates_ConfidenceScore",
                schema: "crm",
                table: "person_duplicates",
                column: "ConfidenceScore");

            migrationBuilder.CreateIndex(
                name: "IX_person_duplicates_Person2Id",
                schema: "crm",
                table: "person_duplicates",
                column: "Person2Id");

            migrationBuilder.CreateIndex(
                name: "IX_person_duplicates_tenant_id_Person1Id_Person2Id",
                schema: "crm",
                table: "person_duplicates",
                columns: new[] { "tenant_id", "Person1Id", "Person2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_previous_names_NormalizedLastName",
                schema: "crm",
                table: "person_previous_names",
                column: "NormalizedLastName");

            migrationBuilder.CreateIndex(
                name: "IX_person_previous_names_PersonId",
                schema: "crm",
                table: "person_previous_names",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_person_previous_names_tenant_id_PersonId_NormalizedLastName",
                schema: "crm",
                table: "person_previous_names",
                columns: new[] { "tenant_id", "PersonId", "NormalizedLastName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_redirects_ExternalRecordGuid",
                schema: "crm",
                table: "person_redirects",
                column: "ExternalRecordGuid");

            migrationBuilder.CreateIndex(
                name: "IX_person_redirects_TargetPersonId",
                schema: "crm",
                table: "person_redirects",
                column: "TargetPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_person_redirects_tenant_id_SourcePersonId",
                schema: "crm",
                table: "person_redirects",
                columns: new[] { "tenant_id", "SourcePersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_search_keys_NormalizedSearchValue",
                schema: "crm",
                table: "person_search_keys",
                column: "NormalizedSearchValue");

            migrationBuilder.CreateIndex(
                name: "IX_person_search_keys_PersonId",
                schema: "crm",
                table: "person_search_keys",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_person_search_keys_tenant_id_PersonId_KeyType_NormalizedSea~",
                schema: "crm",
                table: "person_search_keys",
                columns: new[] { "tenant_id", "PersonId", "KeyType", "NormalizedSearchValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_person_signals_PersonId",
                schema: "crm",
                table: "person_signals",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_person_signals_SignalTypeId",
                schema: "crm",
                table: "person_signals",
                column: "SignalTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_person_signals_tenant_id",
                schema: "crm",
                table: "person_signals",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_person_viewed_TargetPersonId_ViewedOnUtc",
                schema: "crm",
                table: "person_viewed",
                columns: new[] { "TargetPersonId", "ViewedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_person_viewed_tenant_id",
                schema: "crm",
                table: "person_viewed",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_phone_numbers_E164Number",
                schema: "crm",
                table: "phone_numbers",
                column: "E164Number");

            migrationBuilder.CreateIndex(
                name: "IX_phone_numbers_Number",
                schema: "crm",
                table: "phone_numbers",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_phone_numbers_NumberReversed",
                schema: "crm",
                table: "phone_numbers",
                column: "NumberReversed");

            migrationBuilder.CreateIndex(
                name: "IX_phone_numbers_PersonId",
                schema: "crm",
                table: "phone_numbers",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_phone_numbers_tenant_id",
                schema: "crm",
                table: "phone_numbers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_signal_types_Order",
                schema: "crm",
                table: "signal_types",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_signal_types_tenant_id_NormalizedName",
                schema: "crm",
                table: "signal_types",
                columns: new[] { "tenant_id", "NormalizedName" },
                unique: true);
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(migrationBuilder, "crm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessment_types",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "assessments",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "background_checks",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "badge_definitions",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "household_locations",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "household_members",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "identity_verification_codes",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "identity_verifications",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "known_relationships",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "meta_first_name_gender_lookups",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "meta_nick_name_lookups",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "metaphones",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "peer_network_edges",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "person_duplicates",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "person_previous_names",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "person_redirects",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "person_search_keys",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "person_signals",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "person_viewed",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "phone_numbers",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "signal_types",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "households",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "people",
                schema: "crm");
        }
    }
}
