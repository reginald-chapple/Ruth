using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Events.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialEventsMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.CreateTable(
                name: "event_calendars",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IconCssClass = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsIndexEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_calendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "event_items",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PhotoBinaryFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    DetailsUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ApprovedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "events",
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
                name: "interactive_sessions",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JoinCode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    EventItemOccurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModeratorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ActiveActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveOccurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interactive_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "events",
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
                name: "registration_instances",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    MaxAttendees = table.Column<int>(type: "integer", nullable: true),
                    RegistrationOpensUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RegistrationClosesUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PaymentDeadlineDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CostOverridePerRegistrant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DepositOverridePerRegistrant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    EventItemOccurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedTargetGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    AdditionalInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "registration_sessions",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestedSeatCount = table.Column<int>(type: "integer", nullable: false),
                    HeldConfirmedSeats = table.Column<int>(type: "integer", nullable: false),
                    HeldWaitlistSeats = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "registration_templates",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultFinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    BaseCostPerRegistrant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumInitialDepositPerRegistrant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentModel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WaitlistEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SeatHoldTtlMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxRegistrantsPerSubmission = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "registrations",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmationCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RegistrarPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegistrarFirstName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RegistrarLastName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RegistrarEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AppliedDiscountCode = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: true),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountTotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LinkedInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "event_calendar_items",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarId = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_calendar_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_calendar_items_event_items_EventItemId",
                        column: x => x.EventItemId,
                        principalSchema: "events",
                        principalTable: "event_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_item_audiences",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AudienceDefinedValueId = table.Column<Guid>(type: "uuid", nullable: true),
                    AudienceLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_item_audiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_item_audiences_event_items_EventItemId",
                        column: x => x.EventItemId,
                        principalSchema: "events",
                        principalTable: "event_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_item_occurrences",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationDescription = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CustomEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TimeZoneId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ContactPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    OccurrenceNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RegistrationInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_item_occurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_item_occurrences_event_items_EventItemId",
                        column: x => x.EventItemId,
                        principalSchema: "events",
                        principalTable: "event_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interactive_actions",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PromptText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    options = table.Column<string>(type: "jsonb", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    AllowMultipleResponses = table.Column<bool>(type: "boolean", nullable: false),
                    RequireModeration = table.Column<bool>(type: "boolean", nullable: false),
                    ShowLiveResultsOnStage = table.Column<bool>(type: "boolean", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interactive_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interactive_actions_interactive_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "events",
                        principalTable: "interactive_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interactive_responses",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParticipantToken = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ResponderPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelectedOption = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    TextResponse = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ModerationStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Upvotes = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interactive_responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interactive_responses_interactive_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "events",
                        principalTable: "interactive_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interactive_session_occurrences",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventItemOccurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interactive_session_occurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interactive_session_occurrences_interactive_sessions_Sessio~",
                        column: x => x.SessionId,
                        principalSchema: "events",
                        principalTable: "interactive_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registration_template_discounts",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    DiscountKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MaxRegistrants = table.Column<int>(type: "integer", nullable: true),
                    MinRegistrants = table.Column<int>(type: "integer", nullable: true),
                    IsAutoApplyEarlyBird = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_template_discounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_template_discounts_registration_templates_Temp~",
                        column: x => x.TemplateId,
                        principalSchema: "events",
                        principalTable: "registration_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registration_template_fees",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FeeType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDiscount = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_template_fees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_template_fees_registration_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "events",
                        principalTable: "registration_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registration_template_forms",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_template_forms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_template_forms_registration_templates_Template~",
                        column: x => x.TemplateId,
                        principalSchema: "events",
                        principalTable: "registration_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registration_template_placements",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    GroupTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultTargetGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultGroupRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    AutoPlaceOnConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_template_placements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_template_placements_registration_templates_Tem~",
                        column: x => x.TemplateId,
                        principalSchema: "events",
                        principalTable: "registration_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registration_payment_records",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_payment_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_payment_records_registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalSchema: "events",
                        principalTable: "registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registration_registrants",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LastName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    WaitlistPosition = table.Column<int>(type: "integer", nullable: true),
                    BaseCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FeesTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlacedGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlacedGroupMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    field_values = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_registrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_registrants_registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalSchema: "events",
                        principalTable: "registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registration_template_fee_items",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    UnitAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxQuantityAvailable = table.Column<int>(type: "integer", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_template_fee_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_template_fee_items_registration_template_fees_~",
                        column: x => x.FeeId,
                        principalSchema: "events",
                        principalTable: "registration_template_fees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registration_template_form_fields",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FieldKind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    select_options = table.Column<string>(type: "jsonb", nullable: false),
                    ShowWhenFieldKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ShowWhenEqualsValue = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_template_form_fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_template_form_fields_registration_template_for~",
                        column: x => x.FormId,
                        principalSchema: "events",
                        principalTable: "registration_template_forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registration_registrant_fee_selections",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_registrant_fee_selections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_registrant_fee_selections_registration_registr~",
                        column: x => x.RegistrantId,
                        principalSchema: "events",
                        principalTable: "registration_registrants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_event_calendar_items_EventItemId",
                schema: "events",
                table: "event_calendar_items",
                column: "EventItemId");

            migrationBuilder.CreateIndex(
                name: "IX_event_calendar_items_tenant_id_CalendarId_EventItemId",
                schema: "events",
                table: "event_calendar_items",
                columns: new[] { "tenant_id", "CalendarId", "EventItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_calendars_IsPublic_IsActive",
                schema: "events",
                table: "event_calendars",
                columns: new[] { "IsPublic", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_event_calendars_tenant_id_Slug",
                schema: "events",
                table: "event_calendars",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_item_audiences_AudienceDefinedValueId",
                schema: "events",
                table: "event_item_audiences",
                column: "AudienceDefinedValueId");

            migrationBuilder.CreateIndex(
                name: "IX_event_item_audiences_EventItemId",
                schema: "events",
                table: "event_item_audiences",
                column: "EventItemId");

            migrationBuilder.CreateIndex(
                name: "IX_event_item_audiences_tenant_id",
                schema: "events",
                table: "event_item_audiences",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_item_occurrences_BranchId",
                schema: "events",
                table: "event_item_occurrences",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_event_item_occurrences_ContactPersonId",
                schema: "events",
                table: "event_item_occurrences",
                column: "ContactPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_event_item_occurrences_EventItemId",
                schema: "events",
                table: "event_item_occurrences",
                column: "EventItemId");

            migrationBuilder.CreateIndex(
                name: "IX_event_item_occurrences_ScheduleId",
                schema: "events",
                table: "event_item_occurrences",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_event_item_occurrences_tenant_id",
                schema: "events",
                table: "event_item_occurrences",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_items_ApprovalStatus_IsActive_IsPublic",
                schema: "events",
                table: "event_items",
                columns: new[] { "ApprovalStatus", "IsActive", "IsPublic" });

            migrationBuilder.CreateIndex(
                name: "IX_event_items_attribute_values",
                schema: "events",
                table: "event_items",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_event_items_tenant_id_Slug",
                schema: "events",
                table: "event_items",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interactive_actions_SessionId_DisplayOrder",
                schema: "events",
                table: "interactive_actions",
                columns: new[] { "SessionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_interactive_actions_tenant_id",
                schema: "events",
                table: "interactive_actions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_interactive_responses_ResponderPersonId",
                schema: "events",
                table: "interactive_responses",
                column: "ResponderPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_interactive_responses_SessionId_ActionId",
                schema: "events",
                table: "interactive_responses",
                columns: new[] { "SessionId", "ActionId" });

            migrationBuilder.CreateIndex(
                name: "IX_interactive_responses_tenant_id",
                schema: "events",
                table: "interactive_responses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_interactive_session_occurrences_SessionId",
                schema: "events",
                table: "interactive_session_occurrences",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_interactive_session_occurrences_tenant_id",
                schema: "events",
                table: "interactive_session_occurrences",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_interactive_sessions_Status",
                schema: "events",
                table: "interactive_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_interactive_sessions_tenant_id_JoinCode",
                schema: "events",
                table: "interactive_sessions",
                columns: new[] { "tenant_id", "JoinCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "events",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_registration_instances_BranchId",
                schema: "events",
                table: "registration_instances",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_instances_TemplateId_IsActive",
                schema: "events",
                table: "registration_instances",
                columns: new[] { "TemplateId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_registration_instances_tenant_id_Slug",
                schema: "events",
                table: "registration_instances",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registration_payment_records_RegistrationId",
                schema: "events",
                table: "registration_payment_records",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_payment_records_tenant_id",
                schema: "events",
                table: "registration_payment_records",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_registrant_fee_selections_FeeItemId",
                schema: "events",
                table: "registration_registrant_fee_selections",
                column: "FeeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_registrant_fee_selections_RegistrantId",
                schema: "events",
                table: "registration_registrant_fee_selections",
                column: "RegistrantId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_registrant_fee_selections_tenant_id",
                schema: "events",
                table: "registration_registrant_fee_selections",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_registrants_field_values",
                schema: "events",
                table: "registration_registrants",
                column: "field_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_registration_registrants_InstanceId_Status_WaitlistPosition",
                schema: "events",
                table: "registration_registrants",
                columns: new[] { "InstanceId", "Status", "WaitlistPosition" });

            migrationBuilder.CreateIndex(
                name: "IX_registration_registrants_PersonId",
                schema: "events",
                table: "registration_registrants",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_registrants_RegistrationId",
                schema: "events",
                table: "registration_registrants",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_registrants_tenant_id",
                schema: "events",
                table: "registration_registrants",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_sessions_InstanceId_Status_ExpiresAtUtc",
                schema: "events",
                table: "registration_sessions",
                columns: new[] { "InstanceId", "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_registration_sessions_tenant_id_SessionToken",
                schema: "events",
                table: "registration_sessions",
                columns: new[] { "tenant_id", "SessionToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_discounts_TemplateId",
                schema: "events",
                table: "registration_template_discounts",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_discounts_tenant_id_TemplateId_Code",
                schema: "events",
                table: "registration_template_discounts",
                columns: new[] { "tenant_id", "TemplateId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_fee_items_FeeId_DisplayOrder",
                schema: "events",
                table: "registration_template_fee_items",
                columns: new[] { "FeeId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_fee_items_tenant_id",
                schema: "events",
                table: "registration_template_fee_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_fees_TemplateId_DisplayOrder",
                schema: "events",
                table: "registration_template_fees",
                columns: new[] { "TemplateId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_fees_tenant_id",
                schema: "events",
                table: "registration_template_fees",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_form_fields_FormId_DisplayOrder",
                schema: "events",
                table: "registration_template_form_fields",
                columns: new[] { "FormId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_form_fields_tenant_id",
                schema: "events",
                table: "registration_template_form_fields",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_forms_TemplateId_DisplayOrder",
                schema: "events",
                table: "registration_template_forms",
                columns: new[] { "TemplateId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_forms_tenant_id",
                schema: "events",
                table: "registration_template_forms",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_placements_TemplateId",
                schema: "events",
                table: "registration_template_placements",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_template_placements_tenant_id",
                schema: "events",
                table: "registration_template_placements",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_registration_templates_IsActive",
                schema: "events",
                table: "registration_templates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_registration_templates_tenant_id",
                schema: "events",
                table: "registration_templates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_InstanceId",
                schema: "events",
                table: "registrations",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_RegistrarPersonId",
                schema: "events",
                table: "registrations",
                column: "RegistrarPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_tenant_id_ConfirmationCode",
                schema: "events",
                table: "registrations",
                columns: new[] { "tenant_id", "ConfirmationCode" },
                unique: true);
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(
                migrationBuilder,
                "events"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_calendar_items",
                schema: "events");

            migrationBuilder.DropTable(
                name: "event_calendars",
                schema: "events");

            migrationBuilder.DropTable(
                name: "event_item_audiences",
                schema: "events");

            migrationBuilder.DropTable(
                name: "event_item_occurrences",
                schema: "events");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "events");

            migrationBuilder.DropTable(
                name: "interactive_actions",
                schema: "events");

            migrationBuilder.DropTable(
                name: "interactive_responses",
                schema: "events");

            migrationBuilder.DropTable(
                name: "interactive_session_occurrences",
                schema: "events");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_instances",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_payment_records",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_registrant_fee_selections",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_sessions",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_template_discounts",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_template_fee_items",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_template_form_fields",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_template_placements",
                schema: "events");

            migrationBuilder.DropTable(
                name: "event_items",
                schema: "events");

            migrationBuilder.DropTable(
                name: "interactive_sessions",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_registrants",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_template_fees",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_template_forms",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registrations",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_templates",
                schema: "events");
        }
    }
}
