using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentityMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

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
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_scopes", x => x.id);
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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_auth_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "identity",
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
                    occurred_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_history_entries", x => x.id);
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
                    primary_branch_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    primary_branch_short_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_onboarding_completed = table.Column<bool>(type: "boolean", nullable: false),
                    self_registration_locked = table.Column<bool>(type: "boolean", nullable: false),
                    completed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    completed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_remote_authentication_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "security_roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_locked_out = table.Column<bool>(type: "boolean", nullable: false),
                    is_password_change_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    locked_out_until_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_password_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed_password_attempt_window_start_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_activity_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_password_changed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.id);
                });

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
                    consumed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "external_login_links",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider_subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    provider_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    linked_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    revocation_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "identity",
                columns: table => new
                {
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    security_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "IX_account_tokens_tenant_id_token_hash",
                schema: "identity",
                table: "account_tokens",
                columns: new[] { "tenant_id", "token_hash" },
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
                name: "IX_auth_audit_logs_tenant_id",
                schema: "identity",
                table: "auth_audit_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_clients_tenant_id_client_id",
                schema: "identity",
                table: "auth_clients",
                columns: new[] { "tenant_id", "client_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_scopes_tenant_id_name",
                schema: "identity",
                table: "auth_scopes",
                columns: new[] { "tenant_id", "name" },
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
                name: "IX_entity_acl_rules_tenant_id",
                schema: "identity",
                table: "entity_acl_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_acl_rules_user_account_id",
                schema: "identity",
                table: "entity_acl_rules",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_auth_providers_tenant_id_provider_key",
                schema: "identity",
                table: "external_auth_providers",
                columns: new[] { "tenant_id", "provider_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_login_links_tenant_id_provider_key_provider_subject",
                schema: "identity",
                table: "external_login_links",
                columns: new[] { "tenant_id", "provider_key", "provider_subject" },
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
                name: "IX_login_history_entries_tenant_id",
                schema: "identity",
                table: "login_history_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_login_history_entries_user_account_id",
                schema: "identity",
                table: "login_history_entries",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_profiles_tenant_id_singleton_key",
                schema: "identity",
                table: "organization_profiles",
                columns: new[] { "tenant_id", "singleton_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "identity",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_family_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_tenant_id_token_hash",
                schema: "identity",
                table: "refresh_tokens",
                columns: new[] { "tenant_id", "token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_account_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_remote_authentication_sessions_tenant_id_state",
                schema: "identity",
                table: "remote_authentication_sessions",
                columns: new[] { "tenant_id", "state" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rest_api_keys_tenant_id_key_hash",
                schema: "identity",
                table: "rest_api_keys",
                columns: new[] { "tenant_id", "key_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rest_api_keys_user_account_id",
                schema: "identity",
                table: "rest_api_keys",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_security_roles_tenant_id_normalized_name",
                schema: "identity",
                table: "security_roles",
                columns: new[] { "tenant_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_tenant_id_normalized_email",
                schema: "identity",
                table: "user_accounts",
                columns: new[] { "tenant_id", "normalized_email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_tenant_id_normalized_user_name",
                schema: "identity",
                table: "user_accounts",
                columns: new[] { "tenant_id", "normalized_user_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_security_role_id",
                schema: "identity",
                table: "user_roles",
                column: "security_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_tenant_id",
                schema: "identity",
                table: "user_roles",
                column: "tenant_id");
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(migrationBuilder, "identity");
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
                name: "inbox_messages",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "login_history_entries",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "organization_profiles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "outbox_messages",
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
