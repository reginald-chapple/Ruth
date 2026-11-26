using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Tenancy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialTenancyPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "platform",
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
                schema: "platform",
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
                name: "platform_operators",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_normalized = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_operators", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ein = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    irs_subsection_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    state_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    lifecycle_stage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    irs_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    irs_official_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    irs_deductibility_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    irs_verified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    stripe_connect_account_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    stripe_charges_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    stripe_payouts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_for_review_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by_operator_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "irs_exempt_verification_records",
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
                    table.PrimaryKey("PK_irs_exempt_verification_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_irs_exempt_verification_records_tenants_target_tenant_id",
                        column: x => x.target_tenant_id,
                        principalSchema: "platform",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_domains",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hostname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_custom_domain = table.Column<bool>(type: "boolean", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    make_primary_when_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verification_txt_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    verification_txt_value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    verified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_domains", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_domains_tenants_target_tenant_id",
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

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_platform_operators_email_normalized",
                schema: "platform",
                table: "platform_operators",
                column: "email_normalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_domains_hostname",
                schema: "platform",
                table: "tenant_domains",
                column: "hostname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_domains_target_tenant_id",
                schema: "platform",
                table: "tenant_domains",
                column: "target_tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_ein",
                schema: "platform",
                table: "tenants",
                column: "ein",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_lifecycle_stage",
                schema: "platform",
                table: "tenants",
                column: "lifecycle_stage");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_slug",
                schema: "platform",
                table: "tenants",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "irs_exempt_verification_records",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "platform_operators",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "tenant_domains",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "platform");
        }
    }
}
