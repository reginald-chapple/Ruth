using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Communication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W3S7CommunicationFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "communication_flow_conversions",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowInstanceRecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoalKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StepOrderAtConversion = table.Column<int>(type: "integer", nullable: false),
                    ConversionValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ReferenceKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_flow_conversions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_flow_instance_recipients",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmailAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentStepIndex = table.Column<int>(type: "integer", nullable: false),
                    CompletedStepsCount = table.Column<int>(type: "integer", nullable: false),
                    OpenedStepsCount = table.Column<int>(type: "integer", nullable: false),
                    ClickedStepsCount = table.Column<int>(type: "integer", nullable: false),
                    EnrolledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastStepSentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextStepDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExitedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExitReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    merge_fields = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_flow_instance_recipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_flow_instances",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortLabel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_flow_instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_flows",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TopicKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TriggerMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExitCondition = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ConversionGoalKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ConversionTargetGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConversionGoalUrlPrefix = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConversionWindowDays = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    steps = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_flows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_conversions_FlowId",
                schema: "comms",
                table: "communication_flow_conversions",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_conversions_FlowInstanceRecipientId_Goal~",
                schema: "comms",
                table: "communication_flow_conversions",
                columns: new[] { "FlowInstanceRecipientId", "GoalKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_conversions_PersonId",
                schema: "comms",
                table: "communication_flow_conversions",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instance_recipients_FlowId_Status",
                schema: "comms",
                table: "communication_flow_instance_recipients",
                columns: new[] { "FlowId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instance_recipients_FlowInstanceId",
                schema: "comms",
                table: "communication_flow_instance_recipients",
                column: "FlowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instance_recipients_PersonId",
                schema: "comms",
                table: "communication_flow_instance_recipients",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instance_recipients_Status_NextStepDueAt~",
                schema: "comms",
                table: "communication_flow_instance_recipients",
                columns: new[] { "Status", "NextStepDueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_flow_instances_FlowId_Status",
                schema: "comms",
                table: "communication_flow_instances",
                columns: new[] { "FlowId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_flows_Category_IsActive",
                schema: "comms",
                table: "communication_flows",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_flows_Key",
                schema: "comms",
                table: "communication_flows",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_flows_SystemGuid",
                schema: "comms",
                table: "communication_flows",
                column: "SystemGuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "communication_flow_conversions",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_flow_instance_recipients",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_flow_instances",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_flows",
                schema: "comms");
        }
    }
}
