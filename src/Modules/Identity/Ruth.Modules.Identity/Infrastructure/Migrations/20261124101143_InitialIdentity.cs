using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "identity",
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
                name: "organization_profiles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    singleton_key = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    primary_campus_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    primary_campus_short_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_onboarding_completed = table.Column<bool>(type: "boolean", nullable: false),
                    self_registration_locked = table.Column<bool>(type: "boolean", nullable: false),
                    completed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_profiles", x => x.id);
                    table.CheckConstraint("ck_organization_profiles_singleton_key", "singleton_key = 1");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "identity",
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
                name: "security_roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    is_locked_out = table.Column<bool>(type: "boolean", nullable: false),
                    last_password_changed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "identity",
                columns: table => new
                {
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    security_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_account_id, x.security_role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_security_roles_security_role_id",
                        column: x => x.security_role_id,
                        principalSchema: "identity",
                        principalTable: "security_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalSchema: "identity",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_profiles_singleton_key",
                schema: "identity",
                table: "organization_profiles",
                column: "singleton_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "identity",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_security_roles_normalized_name",
                schema: "identity",
                table: "security_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_normalized_email",
                schema: "identity",
                table: "user_accounts",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_normalized_user_name",
                schema: "identity",
                table: "user_accounts",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_security_role_id",
                schema: "identity",
                table: "user_roles",
                column: "security_role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "organization_profiles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "security_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_accounts",
                schema: "identity");
        }
    }
}
