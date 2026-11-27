using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Tenancy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W7S2TenancyControlPlaneEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "irs_exempt_verification_records",
                schema: "platform");

            migrationBuilder.CreateTable(
                name: "irs_exempt_verifications",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_ein = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    official_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    subsection_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    deductibility_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    diagnostic_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irs_exempt_verifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_irs_exempt_verifications_tenants_target_tenant_id",
                        column: x => x.target_tenant_id,
                        principalSchema: "platform",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stripe_connect_accounts",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_account_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_mock_account = table.Column<bool>(type: "boolean", nullable: false),
                    charges_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    payouts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    details_submitted = table.Column<bool>(type: "boolean", nullable: false),
                    last_onboarding_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    onboarding_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_synced_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_connect_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_stripe_connect_accounts_tenants_target_tenant_id",
                        column: x => x.target_tenant_id,
                        principalSchema: "platform",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_irs_exempt_verifications_target_tenant_id_created_on_utc",
                schema: "platform",
                table: "irs_exempt_verifications",
                columns: new[] { "target_tenant_id", "created_on_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_stripe_connect_accounts_external_account_id",
                schema: "platform",
                table: "stripe_connect_accounts",
                column: "external_account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stripe_connect_accounts_target_tenant_id",
                schema: "platform",
                table: "stripe_connect_accounts",
                column: "target_tenant_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "irs_exempt_verifications",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "stripe_connect_accounts",
                schema: "platform");

            migrationBuilder.CreateTable(
                name: "irs_exempt_verification_records",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_ein = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deductibility_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    diagnostic_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    official_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subsection_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    target_tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irs_exempt_verification_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_irs_exempt_verification_records_tenants_target_tenant_id",
                        column: x => x.target_tenant_id,
                        principalSchema: "platform",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_irs_exempt_verification_records_target_tenant_id_created_on~",
                schema: "platform",
                table: "irs_exempt_verification_records",
                columns: new[] { "target_tenant_id", "created_on_utc" });
        }
    }
}
