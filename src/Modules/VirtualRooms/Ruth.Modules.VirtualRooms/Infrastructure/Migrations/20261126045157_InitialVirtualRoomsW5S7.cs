using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.VirtualRooms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialVirtualRoomsW5S7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "virtual");

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "virtual",
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
                name: "outbox_messages",
                schema: "virtual",
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
                name: "virtual_room_sessions",
                schema: "virtual",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VirtualRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    LiveKitRoomSid = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ScheduledStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScheduledEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActualStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_virtual_room_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "virtual_rooms",
                schema: "virtual",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    LiveKitRoomName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContextKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LinkedGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedBranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedBookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedSupportRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedEventItemOccurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedLmsClassId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalReferenceKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    HostModeratorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    EmptyTimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    AutoRecordAttendance = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumAttendanceDwellSeconds = table.Column<int>(type: "integer", nullable: false),
                    AutoStartRecordingEgress = table.Column<bool>(type: "boolean", nullable: false),
                    IsWebinarBroadcast = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_virtual_rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "virtual_room_participant_sessions",
                schema: "virtual",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantIdentity = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FirstJoinedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastJoinedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLeftAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsCurrentlyConnected = table.Column<bool>(type: "boolean", nullable: false),
                    JoinCount = table.Column<int>(type: "integer", nullable: false),
                    CumulativeDwellSeconds = table.Column<int>(type: "integer", nullable: false),
                    QualifiedForAttendance = table.Column<bool>(type: "boolean", nullable: false),
                    AttendanceRecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_virtual_room_participant_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_virtual_room_participant_sessions_virtual_room_sessions_Ses~",
                        column: x => x.SessionId,
                        principalSchema: "virtual",
                        principalTable: "virtual_room_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "virtual_room_recording_archives",
                schema: "virtual",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EgressId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StorageBucket = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StorageObjectKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TranscriptObjectKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_virtual_room_recording_archives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_virtual_room_recording_archives_virtual_room_sessions_Sessi~",
                        column: x => x.SessionId,
                        principalSchema: "virtual",
                        principalTable: "virtual_room_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "virtual",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_room_participant_sessions_PersonId",
                schema: "virtual",
                table: "virtual_room_participant_sessions",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_room_participant_sessions_SessionId_ParticipantIden~",
                schema: "virtual",
                table: "virtual_room_participant_sessions",
                columns: new[] { "SessionId", "ParticipantIdentity" });

            migrationBuilder.CreateIndex(
                name: "IX_virtual_room_recording_archives_EgressId",
                schema: "virtual",
                table: "virtual_room_recording_archives",
                column: "EgressId");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_room_recording_archives_SessionId_Status",
                schema: "virtual",
                table: "virtual_room_recording_archives",
                columns: new[] { "SessionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_virtual_room_sessions_LiveKitRoomSid",
                schema: "virtual",
                table: "virtual_room_sessions",
                column: "LiveKitRoomSid");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_room_sessions_VirtualRoomId_Status",
                schema: "virtual",
                table: "virtual_room_sessions",
                columns: new[] { "VirtualRoomId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_virtual_rooms_attribute_values",
                schema: "virtual",
                table: "virtual_rooms",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_rooms_ExternalReferenceKey",
                schema: "virtual",
                table: "virtual_rooms",
                column: "ExternalReferenceKey");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_rooms_LinkedBookingId",
                schema: "virtual",
                table: "virtual_rooms",
                column: "LinkedBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_rooms_LinkedGroupId",
                schema: "virtual",
                table: "virtual_rooms",
                column: "LinkedGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_rooms_LiveKitRoomName",
                schema: "virtual",
                table: "virtual_rooms",
                column: "LiveKitRoomName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_virtual_rooms_Slug",
                schema: "virtual",
                table: "virtual_rooms",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "virtual");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "virtual");

            migrationBuilder.DropTable(
                name: "virtual_room_participant_sessions",
                schema: "virtual");

            migrationBuilder.DropTable(
                name: "virtual_room_recording_archives",
                schema: "virtual");

            migrationBuilder.DropTable(
                name: "virtual_rooms",
                schema: "virtual");

            migrationBuilder.DropTable(
                name: "virtual_room_sessions",
                schema: "virtual");
        }
    }
}
