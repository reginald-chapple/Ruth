using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Events.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W4S7CalendarsAndInteractiveSessions : Migration
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
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
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.message_id, x.consumer_name });
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "event_calendar_items",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    AudienceLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
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
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false)
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
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false)
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
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_event_calendar_items_CalendarId_EventItemId",
                schema: "events",
                table: "event_calendar_items",
                columns: new[] { "CalendarId", "EventItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_calendar_items_EventItemId",
                schema: "events",
                table: "event_calendar_items",
                column: "EventItemId");

            migrationBuilder.CreateIndex(
                name: "IX_event_calendars_IsPublic_IsActive",
                schema: "events",
                table: "event_calendars",
                columns: new[] { "IsPublic", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_event_calendars_Slug",
                schema: "events",
                table: "event_calendars",
                column: "Slug",
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
                name: "IX_event_items_Slug",
                schema: "events",
                table: "event_items",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interactive_actions_SessionId_DisplayOrder",
                schema: "events",
                table: "interactive_actions",
                columns: new[] { "SessionId", "DisplayOrder" });

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
                name: "IX_interactive_session_occurrences_SessionId",
                schema: "events",
                table: "interactive_session_occurrences",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_interactive_sessions_JoinCode",
                schema: "events",
                table: "interactive_sessions",
                column: "JoinCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interactive_sessions_Status",
                schema: "events",
                table: "interactive_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "events",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");
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
                name: "event_items",
                schema: "events");

            migrationBuilder.DropTable(
                name: "interactive_sessions",
                schema: "events");
        }
    }
}
