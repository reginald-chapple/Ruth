using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWave1AuthAndAcl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_password_attempt_count",
                schema: "identity",
                table: "user_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_password_attempt_window_start_on_utc",
                schema: "identity",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "identity",
                table: "user_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_password_change_required",
                schema: "identity",
                table: "user_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_activity_on_utc",
                schema: "identity",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_login_on_utc",
                schema: "identity",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_out_until_on_utc",
                schema: "identity",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "identity",
                table: "security_roles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "identity",
                table: "security_roles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "account_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_tokens_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalSchema: "identity",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_audit_logs",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    change_type = table.Column<int>(type: "integer", nullable: false),
                    changed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pre_decision = table.Column<int>(type: "integer", nullable: true),
                    post_decision = table.Column<int>(type: "integer", nullable: true),
                    pre_order = table.Column<int>(type: "integer", nullable: true),
                    post_order = table.Column<int>(type: "integer", nullable: true),
                    special_role = table.Column<int>(type: "integer", nullable: false),
                    security_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auth_clients",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    client_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    client_secret_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    redirect_uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    post_logout_redirect_uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    allowed_scopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    allowed_claims = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    allow_user_api_access = table.Column<bool>(type: "boolean", nullable: false),
                    scope_approval_expiration_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auth_scopes",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    public_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entity_acl_rules",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    decision = table.Column<int>(type: "integer", nullable: false),
                    special_role = table.Column<int>(type: "integer", nullable: false),
                    security_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_acl_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_entity_acl_rules_security_roles_security_role_id",
                        column: x => x.security_role_id,
                        principalSchema: "identity",
                        principalTable: "security_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_entity_acl_rules_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalSchema: "identity",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "external_auth_providers",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    authority = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    client_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    scopes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    auto_link_by_email = table.Column<bool>(type: "boolean", nullable: false),
                    callback_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_auth_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_login_links",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider_subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    provider_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    linked_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_login_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_login_links_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalSchema: "identity",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "login_history_entries",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name_attempted = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    auth_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    occurred_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_history_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    revoked_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalSchema: "identity",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "remote_authentication_sessions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    state = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    nonce = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    code_verifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    return_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_remote_authentication_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rest_api_keys",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    key_prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    expires_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_used_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rest_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "FK_rest_api_keys_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalSchema: "identity",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_tokens_token_hash",
                schema: "identity",
                table: "account_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_tokens_user_account_id",
                schema: "identity",
                table: "account_tokens",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_audit_logs_changed_on_utc",
                schema: "identity",
                table: "auth_audit_logs",
                column: "changed_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_auth_audit_logs_entity_type_key_entity_id",
                schema: "identity",
                table: "auth_audit_logs",
                columns: new[] { "entity_type_key", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_auth_clients_client_id",
                schema: "identity",
                table: "auth_clients",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_scopes_name",
                schema: "identity",
                table: "auth_scopes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entity_acl_rules_entity_type_key_entity_id_action_order",
                schema: "identity",
                table: "entity_acl_rules",
                columns: new[] { "entity_type_key", "entity_id", "action", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_entity_acl_rules_security_role_id",
                schema: "identity",
                table: "entity_acl_rules",
                column: "security_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_acl_rules_user_account_id",
                schema: "identity",
                table: "entity_acl_rules",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_auth_providers_provider_key",
                schema: "identity",
                table: "external_auth_providers",
                column: "provider_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_login_links_provider_key_provider_subject",
                schema: "identity",
                table: "external_login_links",
                columns: new[] { "provider_key", "provider_subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_login_links_user_account_id",
                schema: "identity",
                table: "external_login_links",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_login_history_entries_occurred_on_utc",
                schema: "identity",
                table: "login_history_entries",
                column: "occurred_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_login_history_entries_user_account_id",
                schema: "identity",
                table: "login_history_entries",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_family_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                schema: "identity",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_account_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_remote_authentication_sessions_state",
                schema: "identity",
                table: "remote_authentication_sessions",
                column: "state",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rest_api_keys_key_hash",
                schema: "identity",
                table: "rest_api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rest_api_keys_user_account_id",
                schema: "identity",
                table: "rest_api_keys",
                column: "user_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "auth_audit_logs",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "auth_clients",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "auth_scopes",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "entity_acl_rules",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "external_auth_providers",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "external_login_links",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "login_history_entries",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "remote_authentication_sessions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "rest_api_keys",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "failed_password_attempt_count",
                schema: "identity",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "failed_password_attempt_window_start_on_utc",
                schema: "identity",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "identity",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "is_password_change_required",
                schema: "identity",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "last_activity_on_utc",
                schema: "identity",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "last_login_on_utc",
                schema: "identity",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "locked_out_until_on_utc",
                schema: "identity",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "identity",
                table: "security_roles");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "identity",
                table: "security_roles");
        }
    }
}
