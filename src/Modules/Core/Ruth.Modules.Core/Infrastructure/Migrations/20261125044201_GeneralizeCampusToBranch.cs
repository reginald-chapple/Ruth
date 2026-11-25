using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeCampusToBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campus_schedules",
                schema: "core");

            migrationBuilder.DropTable(
                name: "campus_topics",
                schema: "core");

            migrationBuilder.DropTable(
                name: "campuses",
                schema: "core");

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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
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
                    order = table.Column<int>(type: "integer", nullable: false)
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
                    is_public = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_branch_schedules_branch_id_order",
                schema: "core",
                table: "branch_schedules",
                columns: new[] { "branch_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_branch_topics_branch_id",
                schema: "core",
                table: "branch_topics",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_branches_normalized_name",
                schema: "core",
                table: "branches",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branches_system_guid",
                schema: "core",
                table: "branches",
                column: "system_guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_schedules",
                schema: "core");

            migrationBuilder.DropTable(
                name: "branch_topics",
                schema: "core");

            migrationBuilder.DropTable(
                name: "branches",
                schema: "core");

            migrationBuilder.CreateTable(
                name: "campuses",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    average_weekend_attendance = table.Column<int>(type: "integer", nullable: true),
                    beacon_id = table.Column<int>(type: "integer", nullable: true),
                    campus_status_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    campus_type_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    closed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    leader_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    opened_date = table.Column<DateOnly>(type: "date", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    short_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    system_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    team_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    time_zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    tithe_metric = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campus_schedules",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_type_value_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campus_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_campus_schedules_campuses_campus_id",
                        column: x => x.campus_id,
                        principalSchema: "core",
                        principalTable: "campuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campus_topics",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    topic_type_value_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campus_topics", x => x.id);
                    table.ForeignKey(
                        name: "FK_campus_topics_campuses_campus_id",
                        column: x => x.campus_id,
                        principalSchema: "core",
                        principalTable: "campuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_campus_schedules_campus_id_order",
                schema: "core",
                table: "campus_schedules",
                columns: new[] { "campus_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_campus_topics_campus_id",
                schema: "core",
                table: "campus_topics",
                column: "campus_id");

            migrationBuilder.CreateIndex(
                name: "IX_campuses_normalized_name",
                schema: "core",
                table: "campuses",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_campuses_system_guid",
                schema: "core",
                table: "campuses",
                column: "system_guid",
                unique: true);
        }
    }
}
