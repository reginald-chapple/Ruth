using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.CheckIn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W3S8SmartWalletPasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issued_wallet_passes",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PassTypeIdentifier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ScanToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AuthTokenSalt = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AuthTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClaimTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CardStyle = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    UpdatedSequence = table.Column<long>(type: "bigint", nullable: false),
                    dynamic_fields = table.Column<string>(type: "jsonb", nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issued_wallet_passes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_pass_device_registrations",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedPassId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceLibraryIdentifier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PassTypeIdentifier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PushToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RegisteredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastNotifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_pass_device_registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_pass_templates",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PassTypeIdentifier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OrganizationName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CardStyle = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BackgroundColorHex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ForegroundColorHex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LabelColorHex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LogoText = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DefaultValidityDays = table.Column<int>(type: "integer", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_pass_templates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_issued_wallet_passes_ClaimTokenHash",
                schema: "checkin",
                table: "issued_wallet_passes",
                column: "ClaimTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issued_wallet_passes_PassTypeIdentifier_SerialNumber",
                schema: "checkin",
                table: "issued_wallet_passes",
                columns: new[] { "PassTypeIdentifier", "SerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issued_wallet_passes_PersonId",
                schema: "checkin",
                table: "issued_wallet_passes",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_issued_wallet_passes_ScanToken",
                schema: "checkin",
                table: "issued_wallet_passes",
                column: "ScanToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issued_wallet_passes_SerialNumber",
                schema: "checkin",
                table: "issued_wallet_passes",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issued_wallet_passes_TemplateId",
                schema: "checkin",
                table: "issued_wallet_passes",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_pass_device_registrations_DeviceLibraryIdentifier_P~1",
                schema: "checkin",
                table: "wallet_pass_device_registrations",
                columns: new[] { "DeviceLibraryIdentifier", "PassTypeIdentifier", "SerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_pass_device_registrations_DeviceLibraryIdentifier_Pa~",
                schema: "checkin",
                table: "wallet_pass_device_registrations",
                columns: new[] { "DeviceLibraryIdentifier", "PassTypeIdentifier" });

            migrationBuilder.CreateIndex(
                name: "IX_wallet_pass_device_registrations_IssuedPassId",
                schema: "checkin",
                table: "wallet_pass_device_registrations",
                column: "IssuedPassId");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_pass_templates_Key",
                schema: "checkin",
                table: "wallet_pass_templates",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_pass_templates_SystemGuid",
                schema: "checkin",
                table: "wallet_pass_templates",
                column: "SystemGuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issued_wallet_passes",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "wallet_pass_device_registrations",
                schema: "checkin");

            migrationBuilder.DropTable(
                name: "wallet_pass_templates",
                schema: "checkin");
        }
    }
}
