using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.CheckIn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCheckInMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "checkin");

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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_check_in_sessions", x => x.Id);
                });

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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "check_in_labels",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LabelKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    layout = table.Column<string>(type: "jsonb", nullable: false),
                    CustomZplOverride = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    PrintOrder = table.Column<int>(type: "integer", nullable: false),
                    Copies = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_labels", x => x.Id);
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
                    IssuedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_security_pickup_codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "checkin",
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
                schema: "checkin",
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
                    VerifiedPickupCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    eligibility = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    IsOverflowRoom = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "IX_attendance_check_in_sessions_tenant_id",
                schema: "checkin",
                table: "attendance_check_in_sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_area_locations_CheckInAreaId",
                schema: "checkin",
                table: "check_in_area_locations",
                column: "CheckInAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_area_locations_LocationId",
                schema: "checkin",
                table: "check_in_area_locations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_area_locations_tenant_id_CheckInAreaId_LocationId",
                schema: "checkin",
                table: "check_in_area_locations",
                columns: new[] { "tenant_id", "CheckInAreaId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_check_in_areas_CheckInConfigurationId_Order",
                schema: "checkin",
                table: "check_in_areas",
                columns: new[] { "CheckInConfigurationId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_areas_tenant_id_CheckInConfigurationId_NormalizedN~",
                schema: "checkin",
                table: "check_in_areas",
                columns: new[] { "tenant_id", "CheckInConfigurationId", "NormalizedName" },
                unique: true);

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
                name: "IX_check_in_attendance_entries_tenant_id",
                schema: "checkin",
                table: "check_in_attendance_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_configurations_BranchId",
                schema: "checkin",
                table: "check_in_configurations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_configurations_tenant_id_NormalizedName",
                schema: "checkin",
                table: "check_in_configurations",
                columns: new[] { "tenant_id", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_check_in_labels_ConfigurationId",
                schema: "checkin",
                table: "check_in_labels",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_labels_LabelKind_IsActive",
                schema: "checkin",
                table: "check_in_labels",
                columns: new[] { "LabelKind", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_labels_tenant_id_Key",
                schema: "checkin",
                table: "check_in_labels",
                columns: new[] { "tenant_id", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_security_pickup_codes_tenant_id_ServiceDate_BranchSco~",
                schema: "checkin",
                table: "daily_security_pickup_codes",
                columns: new[] { "tenant_id", "ServiceDate", "BranchScopeKey", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "checkin",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(migrationBuilder, "checkin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_in_area_locations",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "check_in_attendance_entries",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "check_in_labels",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "daily_security_pickup_codes",
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
                name: "attendance_check_in_sessions",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "check_in_configurations",
                schema: "checkin");
        }
    }
}
