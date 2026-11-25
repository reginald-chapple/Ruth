using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.CheckIn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCheckInSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "checkin");

            migrationBuilder.CreateTable(
                name: "check_in_configurations",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProfileKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    SearchMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CheckInStartOffsetMinutes = table.Column<int>(type: "integer", nullable: false),
                    CheckInEndOffsetMinutes = table.Column<int>(type: "integer", nullable: false),
                    SecurityCodeAlphaLength = table.Column<int>(type: "integer", nullable: false),
                    SecurityCodeNumericLength = table.Column<int>(type: "integer", nullable: false),
                    PreventDuplicateCheckIn = table.Column<bool>(type: "boolean", nullable: false),
                    AllowCheckout = table.Column<bool>(type: "boolean", nullable: false),
                    AutoSelectDaysBack = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "checkin",
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
                schema: "checkin",
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
                name: "check_in_areas",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LinkedGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    BalancingStrategy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PrintBadge = table.Column<bool>(type: "boolean", nullable: false),
                    IssueSecurityPickupCode = table.Column<bool>(type: "boolean", nullable: false),
                    eligibility = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_check_in_areas_check_in_configurations_CheckInConfiguration~",
                        column: x => x.CheckInConfigurationId,
                        principalSchema: "checkin",
                        principalTable: "check_in_configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "check_in_area_locations",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    schedule_ids = table.Column<string>(type: "jsonb", nullable: false),
                    SoftCapacity = table.Column<int>(type: "integer", nullable: true),
                    FirmCapacity = table.Column<int>(type: "integer", nullable: true),
                    PriorityOrder = table.Column<int>(type: "integer", nullable: false),
                    IsOpenForCheckIn = table.Column<bool>(type: "boolean", nullable: false),
                    IsOverflowRoom = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_area_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_check_in_area_locations_check_in_areas_CheckInAreaId",
                        column: x => x.CheckInAreaId,
                        principalSchema: "checkin",
                        principalTable: "check_in_areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_area_locations_CheckInAreaId_LocationId",
                schema: "checkin",
                table: "check_in_area_locations",
                columns: new[] { "CheckInAreaId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_check_in_area_locations_LocationId",
                schema: "checkin",
                table: "check_in_area_locations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_areas_CheckInConfigurationId_NormalizedName",
                schema: "checkin",
                table: "check_in_areas",
                columns: new[] { "CheckInConfigurationId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_check_in_areas_CheckInConfigurationId_Order",
                schema: "checkin",
                table: "check_in_areas",
                columns: new[] { "CheckInConfigurationId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_configurations_BranchId",
                schema: "checkin",
                table: "check_in_configurations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_configurations_NormalizedName",
                schema: "checkin",
                table: "check_in_configurations",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "checkin",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_in_area_locations",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "check_in_areas",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "check_in_configurations",
                schema: "checkin");
        }
    }
}
