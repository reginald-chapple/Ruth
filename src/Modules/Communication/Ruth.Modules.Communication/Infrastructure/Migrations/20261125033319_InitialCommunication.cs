using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Communication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommunication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "comms");

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "comms",
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
                schema: "comms",
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
                name: "template_snippets",
                schema: "comms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    documentation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    markup = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    output_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    category_ids = table.Column<string>(type: "jsonb", nullable: false),
                    default_parameters = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_snippets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "comms",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_template_snippets_key",
                schema: "comms",
                table: "template_snippets",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_template_snippets_system_guid",
                schema: "comms",
                table: "template_snippets",
                column: "system_guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "template_snippets",
                schema: "comms");
        }
    }
}
