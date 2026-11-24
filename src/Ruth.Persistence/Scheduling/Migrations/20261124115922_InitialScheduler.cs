using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Persistence.Scheduling.Migrations
{
    /// <inheritdoc />
    public partial class InitialScheduler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduler");

            migrationBuilder.CreateTable(
                name: "scheduled_jobs",
                schema: "scheduler",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cron_expression = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_run_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_successful_run_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    last_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    last_status_message = table.Column<string>(type: "text", nullable: true),
                    last_run_worker_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    history_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 500),
                    next_run_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_job_histories",
                schema: "scheduler",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    started_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_job_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_scheduled_job_histories_scheduled_jobs_scheduled_job_id",
                        column: x => x.scheduled_job_id,
                        principalSchema: "scheduler",
                        principalTable: "scheduled_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_job_histories_scheduled_job_id_started_on_utc",
                schema: "scheduler",
                table: "scheduled_job_histories",
                columns: new[] { "scheduled_job_id", "started_on_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_jobs_is_active_next_run_utc",
                schema: "scheduler",
                table: "scheduled_jobs",
                columns: new[] { "is_active", "next_run_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_jobs_job_key",
                schema: "scheduler",
                table: "scheduled_jobs",
                column: "job_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_job_histories",
                schema: "scheduler");

            migrationBuilder.DropTable(
                name: "scheduled_jobs",
                schema: "scheduler");
        }
    }
}
