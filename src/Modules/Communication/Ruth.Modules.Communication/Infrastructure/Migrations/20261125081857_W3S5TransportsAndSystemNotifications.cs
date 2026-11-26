using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Communication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W3S5TransportsAndSystemNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_devices", x => x.Id);
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
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
                    SentOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_phone_numbers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_personal_devices_PersonId_IsActive_NotificationsEnabled",
                schema: "comms",
                table: "personal_devices",
                columns: new[] { "PersonId", "IsActive", "NotificationsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_personal_devices_PushRegistrationToken",
                schema: "comms",
                table: "personal_devices",
                column: "PushRegistrationToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_communications_Category_IsActive",
                schema: "comms",
                table: "system_communications",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_system_communications_Key",
                schema: "comms",
                table: "system_communications",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_communications_SystemGuid",
                schema: "comms",
                table: "system_communications",
                column: "SystemGuid",
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
                name: "IX_system_phone_numbers_AssignedToPersonId",
                schema: "comms",
                table: "system_phone_numbers",
                column: "AssignedToPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_system_phone_numbers_Number",
                schema: "comms",
                table: "system_phone_numbers",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_phone_numbers_SystemGuid",
                schema: "comms",
                table: "system_phone_numbers",
                column: "SystemGuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_devices",
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
        }
    }
}
