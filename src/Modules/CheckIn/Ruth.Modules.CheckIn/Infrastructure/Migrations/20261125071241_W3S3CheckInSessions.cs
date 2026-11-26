using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.CheckIn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W3S3CheckInSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendance_check_in_sessions",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskDeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    CheckedInByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    SearchModeUsed = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SearchTermSummary = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SecurityPickupCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    MobileExpressToken = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    ClientIpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CompletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_check_in_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "daily_security_pickup_codes",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BranchScopeKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_security_pickup_codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "check_in_attendance_entries",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LinkedGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonFullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    SecurityPickupCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    PrintBadgeRequested = table.Column<bool>(type: "boolean", nullable: false),
                    IsCapacityOverride = table.Column<bool>(type: "boolean", nullable: false),
                    IsEligibilityOverride = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    OverrideReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttendanceNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CheckedInOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CheckedOutOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CheckedOutByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    VerifiedPickupCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_attendance_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_check_in_attendance_entries_attendance_check_in_sessions_Se~",
                        column: x => x.SessionId,
                        principalSchema: "checkin",
                        principalTable: "attendance_check_in_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_check_in_sessions_CheckInConfigurationId_Service~",
                schema: "checkin",
                table: "attendance_check_in_sessions",
                columns: new[] { "CheckInConfigurationId", "ServiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_check_in_sessions_HouseholdId",
                schema: "checkin",
                table: "attendance_check_in_sessions",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_check_in_sessions_MobileExpressToken",
                schema: "checkin",
                table: "attendance_check_in_sessions",
                column: "MobileExpressToken");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_check_in_sessions_ServiceDate_SecurityPickupCode",
                schema: "checkin",
                table: "attendance_check_in_sessions",
                columns: new[] { "ServiceDate", "SecurityPickupCode" });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_attendance_entries_CheckInConfigurationId_ServiceD~",
                schema: "checkin",
                table: "check_in_attendance_entries",
                columns: new[] { "CheckInConfigurationId", "ServiceDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_attendance_entries_LocationId_ServiceDate_Status",
                schema: "checkin",
                table: "check_in_attendance_entries",
                columns: new[] { "LocationId", "ServiceDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_attendance_entries_PersonId_ServiceDate_ScheduleId",
                schema: "checkin",
                table: "check_in_attendance_entries",
                columns: new[] { "PersonId", "ServiceDate", "ScheduleId" });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_attendance_entries_ServiceDate_SecurityPickupCode",
                schema: "checkin",
                table: "check_in_attendance_entries",
                columns: new[] { "ServiceDate", "SecurityPickupCode" });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_attendance_entries_SessionId",
                schema: "checkin",
                table: "check_in_attendance_entries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_security_pickup_codes_ServiceDate_BranchScopeKey_Code",
                schema: "checkin",
                table: "daily_security_pickup_codes",
                columns: new[] { "ServiceDate", "BranchScopeKey", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_in_attendance_entries",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "daily_security_pickup_codes",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "attendance_check_in_sessions",
                schema: "checkin");
        }
    }
}
