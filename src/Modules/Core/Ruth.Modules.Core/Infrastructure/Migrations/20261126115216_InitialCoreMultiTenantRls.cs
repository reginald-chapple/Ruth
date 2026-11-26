using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Ruth.Modules.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCoreMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "attribute_definitions",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type_qualifier_column = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_type_qualifier_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    field_type_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_grid_column = table.Column<bool>(type: "boolean", nullable: false),
                    is_multi_value = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    default_value = table.Column<string>(type: "text", nullable: true),
                    global_value = table.Column<string>(type: "text", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    qualifiers = table.Column<string>(type: "jsonb", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attribute_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attribute_matrix_templates",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_rows = table.Column<int>(type: "integer", nullable: true),
                    maximum_rows = table.Column<int>(type: "integer", nullable: true),
                    formatted_template = table.Column<string>(type: "text", nullable: true),
                    columns = table.Column<string>(type: "jsonb", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attribute_matrix_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "binary_file_types",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    icon_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    storage_provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    requires_view_security = table.Column<bool>(type: "boolean", nullable: false),
                    allow_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    preferred_required = table.Column<bool>(type: "boolean", nullable: false),
                    max_file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    max_width = table.Column<int>(type: "integer", nullable: true),
                    max_height = table.Column<int>(type: "integer", nullable: true),
                    allowed_extensions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cache_control_header_settings = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_binary_file_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "binary_files",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    binary_file_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    storage_provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    object_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    is_temporary = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    parent_entity_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content_last_modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_binary_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    short_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    leader_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_status_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_type_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    opened_date = table.Column<DateOnly>(type: "date", nullable: true),
                    closed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    beacon_id = table.Column<int>(type: "integer", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_qualifier_column = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_type_qualifier_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    icon_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    highlight_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_categories_categories_parent_category_id",
                        column: x => x.parent_category_id,
                        principalSchema: "core",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "defined_types",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    help_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    enable_security_on_values = table.Column<bool>(type: "boolean", nullable: false),
                    categorized_values_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defined_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    device_type_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    normalized_ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    print_from = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    print_to_override = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    printer_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    proxy_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    has_camera = table.Column<bool>(type: "boolean", nullable: false),
                    camera_barcode_configuration_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    kiosk_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_types",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_qualifier_column = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    entity_type_qualifier_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    binary_file_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_selectable = table.Column<bool>(type: "boolean", nullable: false),
                    icon_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    default_document_name_template = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    max_documents_per_entity = table.Column<int>(type: "integer", nullable: true),
                    is_image = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    binary_file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    purpose_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entity_sets",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_entity_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expire_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    purpose_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entity_type_descriptors",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    friendly_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    module_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_entity = table.Column<bool>(type: "boolean", nullable: false),
                    is_secured = table.Column<bool>(type: "boolean", nullable: false),
                    supports_attributes = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_type_descriptors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "history_log_entries",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verb = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    change_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    caption = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    value_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false),
                    related_entity_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_data = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_of_change = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_history_log_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "core",
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
                name: "locations",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    location_type_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    street1 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    street2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    county = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    geo_point = table.Column<Point>(type: "geography(Point, 4326)", nullable: true),
                    geo_fence = table.Column<Polygon>(type: "geography(Polygon, 4326)", nullable: true),
                    is_geo_point_locked = table.Column<bool>(type: "boolean", nullable: false),
                    barcode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    assessor_parcel_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    printer_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    soft_room_threshold = table.Column<int>(type: "integer", nullable: true),
                    firm_room_threshold = table.Column<int>(type: "integer", nullable: true),
                    beacon_id = table.Column<int>(type: "integer", nullable: true),
                    standardize_attempted_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    standardize_attempted_service_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    standardize_attempted_result = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    standardized_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    geocode_attempted_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    geocode_attempted_service_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    geocode_attempted_result = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    geocoded_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "note_types",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_selectable = table.Column<bool>(type: "boolean", nullable: false),
                    icon_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    color = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    requires_approvals = table.Column<bool>(type: "boolean", nullable: false),
                    allows_watching = table.Column<bool>(type: "boolean", nullable: false),
                    allows_replies = table.Column<bool>(type: "boolean", nullable: false),
                    max_reply_depth = table.Column<int>(type: "integer", nullable: true),
                    auto_watch_authors = table.Column<bool>(type: "boolean", nullable: false),
                    allows_attachments = table.Column<bool>(type: "boolean", nullable: false),
                    binary_file_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_mention_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "note_watches",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    watcher_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_watching = table.Column<bool>(type: "boolean", nullable: false),
                    watch_replies = table.Column<bool>(type: "boolean", nullable: false),
                    allow_override = table.Column<bool>(type: "boolean", nullable: false),
                    note_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_watches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    note_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_note_id = table.Column<Guid>(type: "uuid", nullable: true),
                    caption = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    text = table.Column<string>(type: "text", nullable: false),
                    is_alert = table.Column<bool>(type: "boolean", nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    is_private_note = table.Column<bool>(type: "boolean", nullable: false),
                    approval_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    approved_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    edited_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    edited_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "core",
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
                name: "person_preferences",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_key = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    entity_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    value = table.Column<string>(type: "text", nullable: false),
                    is_enduring = table.Column<bool>(type: "boolean", nullable: false),
                    last_accessed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "schedule_category_exclusions",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_category_exclusions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "schedule_dates",
                schema: "core",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    end_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    start_date_key = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_dates", x => new { x.schedule_id, x.start_on_utc });
                });

            migrationBuilder.CreateTable(
                name: "schedules",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    abbreviated_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    icalendar_content = table.Column<string>(type: "text", nullable: true),
                    check_in_start_offset_minutes = table.Column<int>(type: "integer", nullable: true),
                    check_in_end_offset_minutes = table.Column<int>(type: "integer", nullable: true),
                    effective_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    weekly_day_of_week = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    weekly_time_of_day = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    auto_inactivate_when_complete = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "signature_document_templates",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    binary_file_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_term = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    template_markup = table.Column<string>(type: "text", nullable: true),
                    signature_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_valid_in_future = table.Column<bool>(type: "boolean", nullable: false),
                    validity_duration_in_days = table.Column<int>(type: "integer", nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signature_document_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "signature_documents",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    signature_document_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    document_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    applies_to_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    binary_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    signed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    signed_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    signed_by_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    signed_document_text = table.Column<string>(type: "text", nullable: true),
                    signed_client_ip = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    signed_client_user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    signed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    signature_data = table.Column<string>(type: "text", nullable: true),
                    signature_verification_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signature_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    icon_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    background_color = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "branch_schedules",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_type_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_schedules_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "core",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branch_topics",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_type_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_topics", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_topics_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "core",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "defined_values",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    defined_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    attributes = table.Column<string>(type: "jsonb", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defined_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_defined_values_defined_types_defined_type_id",
                        column: x => x.defined_type_id,
                        principalSchema: "core",
                        principalTable: "defined_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_locations",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_device_locations_devices_device_id",
                        column: x => x.device_id,
                        principalSchema: "core",
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_set_items",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    additional_merge_values_json = table.Column<string>(type: "jsonb", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_set_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_entity_set_items_entity_sets_entity_set_id",
                        column: x => x.entity_set_id,
                        principalSchema: "core",
                        principalTable: "entity_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note_attachments",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    binary_file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_note_attachments_notes_note_id",
                        column: x => x.note_id,
                        principalSchema: "core",
                        principalTable: "notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tagged_items",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tagged_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_tagged_items_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "core",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attribute_definitions_entity_type_id_entity_type_qualifier_~",
                schema: "core",
                table: "attribute_definitions",
                columns: new[] { "entity_type_id", "entity_type_qualifier_column", "entity_type_qualifier_value", "normalized_key" });

            migrationBuilder.CreateIndex(
                name: "IX_attribute_definitions_tenant_id_system_guid",
                schema: "core",
                table: "attribute_definitions",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attribute_matrix_templates_tenant_id_system_guid",
                schema: "core",
                table: "attribute_matrix_templates",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_binary_file_types_tenant_id_normalized_name",
                schema: "core",
                table: "binary_file_types",
                columns: new[] { "tenant_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_binary_file_types_tenant_id_system_guid",
                schema: "core",
                table: "binary_file_types",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_binary_files_binary_file_type_id",
                schema: "core",
                table: "binary_files",
                column: "binary_file_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_binary_files_is_temporary_modified_on_utc",
                schema: "core",
                table: "binary_files",
                columns: new[] { "is_temporary", "modified_on_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_binary_files_tenant_id_system_guid",
                schema: "core",
                table: "binary_files",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_schedules_branch_id_order",
                schema: "core",
                table: "branch_schedules",
                columns: new[] { "branch_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_branch_schedules_tenant_id",
                schema: "core",
                table: "branch_schedules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_topics_branch_id",
                schema: "core",
                table: "branch_topics",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_topics_tenant_id",
                schema: "core",
                table: "branch_topics",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_branches_tenant_id_normalized_name",
                schema: "core",
                table: "branches",
                columns: new[] { "tenant_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branches_tenant_id_system_guid",
                schema: "core",
                table: "branches",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_entity_type_id_parent_category_id_order",
                schema: "core",
                table: "categories",
                columns: new[] { "entity_type_id", "parent_category_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_categories_parent_category_id",
                schema: "core",
                table: "categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_tenant_id_system_guid",
                schema: "core",
                table: "categories",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_defined_types_tenant_id_normalized_name",
                schema: "core",
                table: "defined_types",
                columns: new[] { "tenant_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_defined_types_tenant_id_system_guid",
                schema: "core",
                table: "defined_types",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_defined_values_attributes",
                schema: "core",
                table: "defined_values",
                column: "attributes")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_defined_values_defined_type_id_order",
                schema: "core",
                table: "defined_values",
                columns: new[] { "defined_type_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_defined_values_tenant_id_system_guid",
                schema: "core",
                table: "defined_values",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_locations_device_id",
                schema: "core",
                table: "device_locations",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_locations_tenant_id_device_id_location_id",
                schema: "core",
                table: "device_locations",
                columns: new[] { "tenant_id", "device_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devices_tenant_id_device_type_value_id_normalized_ip_address",
                schema: "core",
                table: "devices",
                columns: new[] { "tenant_id", "device_type_value_id", "normalized_ip_address" },
                unique: true,
                filter: "normalized_ip_address IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_devices_tenant_id_normalized_name",
                schema: "core",
                table: "devices",
                columns: new[] { "tenant_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devices_tenant_id_system_guid",
                schema: "core",
                table: "devices",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_types_entity_type_id",
                schema: "core",
                table: "document_types",
                column: "entity_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_types_tenant_id_system_guid",
                schema: "core",
                table: "document_types",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_document_type_id_entity_id",
                schema: "core",
                table: "documents",
                columns: new[] { "document_type_id", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_tenant_id_binary_file_id",
                schema: "core",
                table: "documents",
                columns: new[] { "tenant_id", "binary_file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entity_set_items_entity_set_id",
                schema: "core",
                table: "entity_set_items",
                column: "entity_set_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_set_items_tenant_id_entity_set_id_entity_id",
                schema: "core",
                table: "entity_set_items",
                columns: new[] { "tenant_id", "entity_set_id", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entity_sets_expire_on_utc",
                schema: "core",
                table: "entity_sets",
                column: "expire_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_entity_sets_tenant_id",
                schema: "core",
                table: "entity_sets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_type_descriptors_tenant_id_key",
                schema: "core",
                table: "entity_type_descriptors",
                columns: new[] { "tenant_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entity_type_descriptors_tenant_id_system_guid",
                schema: "core",
                table: "entity_type_descriptors",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_history_log_entries_entity_type_id_entity_id_occurred_on_utc",
                schema: "core",
                table: "history_log_entries",
                columns: new[] { "entity_type_id", "entity_id", "occurred_on_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_history_log_entries_tenant_id",
                schema: "core",
                table: "history_log_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_locations_geo_fence",
                schema: "core",
                table: "locations",
                column: "geo_fence")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_locations_geo_point",
                schema: "core",
                table: "locations",
                column: "geo_point")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_locations_parent_location_id",
                schema: "core",
                table: "locations",
                column: "parent_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_locations_tenant_id_system_guid",
                schema: "core",
                table: "locations",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_note_attachments_note_id",
                schema: "core",
                table: "note_attachments",
                column: "note_id");

            migrationBuilder.CreateIndex(
                name: "IX_note_attachments_tenant_id_note_id_binary_file_id",
                schema: "core",
                table: "note_attachments",
                columns: new[] { "tenant_id", "note_id", "binary_file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_note_types_entity_type_id",
                schema: "core",
                table: "note_types",
                column: "entity_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_note_types_tenant_id_system_guid",
                schema: "core",
                table: "note_types",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_note_watches_tenant_id",
                schema: "core",
                table: "note_watches",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_note_watches_watcher_user_id",
                schema: "core",
                table: "note_watches",
                column: "watcher_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notes_entity_id_note_type_id",
                schema: "core",
                table: "notes",
                columns: new[] { "entity_id", "note_type_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notes_parent_note_id",
                schema: "core",
                table: "notes",
                column: "parent_note_id");

            migrationBuilder.CreateIndex(
                name: "IX_notes_tenant_id",
                schema: "core",
                table: "notes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "core",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_person_preferences_owner_key_entity_type_id_entity_id",
                schema: "core",
                table: "person_preferences",
                columns: new[] { "owner_key", "entity_type_id", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_person_preferences_tenant_id_owner_key_key",
                schema: "core",
                table: "person_preferences",
                columns: new[] { "tenant_id", "owner_key", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_category_exclusions_category_id_start_on_utc_end_o~",
                schema: "core",
                table: "schedule_category_exclusions",
                columns: new[] { "category_id", "start_on_utc", "end_on_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_category_exclusions_tenant_id",
                schema: "core",
                table: "schedule_category_exclusions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_dates_start_date_key",
                schema: "core",
                table: "schedule_dates",
                column: "start_date_key");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_dates_start_on_utc_end_on_utc",
                schema: "core",
                table: "schedule_dates",
                columns: new[] { "start_on_utc", "end_on_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_dates_tenant_id",
                schema: "core",
                table: "schedule_dates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedules_category_id",
                schema: "core",
                table: "schedules",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedules_tenant_id_system_guid",
                schema: "core",
                table: "schedules",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_signature_document_templates_tenant_id_system_guid",
                schema: "core",
                table: "signature_document_templates",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_signature_documents_applies_to_entity_id",
                schema: "core",
                table: "signature_documents",
                column: "applies_to_entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_signature_documents_signature_document_template_id",
                schema: "core",
                table: "signature_documents",
                column: "signature_document_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_signature_documents_tenant_id",
                schema: "core",
                table: "signature_documents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tagged_items_entity_type_id_entity_id",
                schema: "core",
                table: "tagged_items",
                columns: new[] { "entity_type_id", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tagged_items_tag_id",
                schema: "core",
                table: "tagged_items",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_tagged_items_tenant_id_tag_id_entity_type_id_entity_id",
                schema: "core",
                table: "tagged_items",
                columns: new[] { "tenant_id", "tag_id", "entity_type_id", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_normalized_name_owner_user_id_entity_type_id",
                schema: "core",
                table: "tags",
                columns: new[] { "normalized_name", "owner_user_id", "entity_type_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tags_tenant_id_system_guid",
                schema: "core",
                table: "tags",
                columns: new[] { "tenant_id", "system_guid" },
                unique: true);
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(migrationBuilder, "core");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attribute_definitions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "attribute_matrix_templates",
                schema: "core");

            migrationBuilder.DropTable(
                name: "binary_file_types",
                schema: "core");

            migrationBuilder.DropTable(
                name: "binary_files",
                schema: "core");

            migrationBuilder.DropTable(
                name: "branch_schedules",
                schema: "core");

            migrationBuilder.DropTable(
                name: "branch_topics",
                schema: "core");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "core");

            migrationBuilder.DropTable(
                name: "defined_values",
                schema: "core");

            migrationBuilder.DropTable(
                name: "device_locations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "document_types",
                schema: "core");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "core");

            migrationBuilder.DropTable(
                name: "entity_set_items",
                schema: "core");

            migrationBuilder.DropTable(
                name: "entity_type_descriptors",
                schema: "core");

            migrationBuilder.DropTable(
                name: "history_log_entries",
                schema: "core");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "core");

            migrationBuilder.DropTable(
                name: "locations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "note_attachments",
                schema: "core");

            migrationBuilder.DropTable(
                name: "note_types",
                schema: "core");

            migrationBuilder.DropTable(
                name: "note_watches",
                schema: "core");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "core");

            migrationBuilder.DropTable(
                name: "person_preferences",
                schema: "core");

            migrationBuilder.DropTable(
                name: "schedule_category_exclusions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "schedule_dates",
                schema: "core");

            migrationBuilder.DropTable(
                name: "schedules",
                schema: "core");

            migrationBuilder.DropTable(
                name: "signature_document_templates",
                schema: "core");

            migrationBuilder.DropTable(
                name: "signature_documents",
                schema: "core");

            migrationBuilder.DropTable(
                name: "tagged_items",
                schema: "core");

            migrationBuilder.DropTable(
                name: "branches",
                schema: "core");

            migrationBuilder.DropTable(
                name: "defined_types",
                schema: "core");

            migrationBuilder.DropTable(
                name: "devices",
                schema: "core");

            migrationBuilder.DropTable(
                name: "entity_sets",
                schema: "core");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "core");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "core");
        }
    }
}
