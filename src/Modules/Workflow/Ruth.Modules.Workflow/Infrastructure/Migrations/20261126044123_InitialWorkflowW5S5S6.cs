using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialWorkflowW5S5S6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.CreateTable(
                name: "form_builder_templates",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FormHeaderHtml = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    FormFooterHtml = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    ConfirmationTitle = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ConfirmationMessageTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IncludePersonEntrySection = table.Column<bool>(type: "boolean", nullable: false),
                    AllowNotes = table.Column<bool>(type: "boolean", nullable: false),
                    CompletionRedirectUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LinkedWorkflowTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_builder_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "workflow",
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
                schema: "workflow",
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
                name: "workflow_action_forms",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowActionTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormBuilderTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    HeaderMarkup = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    FooterMarkup = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    AllowPersonEntry = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSystemCommunicationKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    buttons = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_action_forms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_triggers",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TriggerKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventTypeKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    WorkflowTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityTypeQualifier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    FilterAttributeKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FilterOperator = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FilterExpectedValue = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    WorkflowNameTemplate = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_triggers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_types",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    WorkTerm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    WorkflowIdPrefix = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPersisted = table.Column<bool>(type: "boolean", nullable: false),
                    LoggingLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProcessingIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextSequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    declared_attributes = table.Column<string>(type: "jsonb", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflows",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowNumber = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StateLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    InitiatorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryEntityTypeKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    PrimaryEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextScheduledProcessAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessingAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    attributes = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_action_form_fields",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowActionFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionKey = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    AttributeKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FieldTypeKey = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "boolean", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HelpText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ColumnSpan = table.Column<int>(type: "integer", nullable: false),
                    visibility_rule = table.Column<string>(type: "jsonb", nullable: true),
                    select_options = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_action_form_fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_action_form_fields_workflow_action_forms_WorkflowA~",
                        column: x => x.WorkflowActionFormId,
                        principalSchema: "workflow",
                        principalTable: "workflow_action_forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_action_form_sections",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowActionFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionKey = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ShowHeadingSeparator = table.Column<bool>(type: "boolean", nullable: false),
                    visibility_rule = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_action_form_sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_action_form_sections_workflow_action_forms_Workflo~",
                        column: x => x.WorkflowActionFormId,
                        principalSchema: "workflow",
                        principalTable: "workflow_action_forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_activity_types",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActivatedWithWorkflow = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultAssignedPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultAssignedGroupId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_activity_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_activity_types_workflow_types_WorkflowTypeId",
                        column: x => x.WorkflowTypeId,
                        principalSchema: "workflow",
                        principalTable: "workflow_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_activities",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowActivityTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    AssignedPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attributes = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_activities_workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalSchema: "workflow",
                        principalTable: "workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_logs",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoggedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_logs_workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalSchema: "workflow",
                        principalTable: "workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_action_types",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowActivityTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActionPluginKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActivityCompletedOnSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    IsWorkflowCompletedOnSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    configuration = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_action_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_action_types_workflow_activity_types_WorkflowActiv~",
                        column: x => x.WorkflowActivityTypeId,
                        principalSchema: "workflow",
                        principalTable: "workflow_activity_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_actions",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowActionTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActionPluginKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    FormActionSelected = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    LastProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_actions_workflow_activities_WorkflowActivityId",
                        column: x => x.WorkflowActivityId,
                        principalSchema: "workflow",
                        principalTable: "workflow_activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_form_builder_templates_Slug",
                schema: "workflow",
                table: "form_builder_templates",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "workflow",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_action_form_fields_WorkflowActionFormId_AttributeK~",
                schema: "workflow",
                table: "workflow_action_form_fields",
                columns: new[] { "WorkflowActionFormId", "AttributeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_action_form_sections_WorkflowActionFormId_SectionK~",
                schema: "workflow",
                table: "workflow_action_form_sections",
                columns: new[] { "WorkflowActionFormId", "SectionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_action_forms_WorkflowActionTypeId",
                schema: "workflow",
                table: "workflow_action_forms",
                column: "WorkflowActionTypeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_action_types_WorkflowActivityTypeId_Key",
                schema: "workflow",
                table: "workflow_action_types",
                columns: new[] { "WorkflowActivityTypeId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_actions_WorkflowActivityId",
                schema: "workflow",
                table: "workflow_actions",
                column: "WorkflowActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_activities_AssignedGroupId_CompletedAtUtc",
                schema: "workflow",
                table: "workflow_activities",
                columns: new[] { "AssignedGroupId", "CompletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_activities_AssignedPersonId_CompletedAtUtc",
                schema: "workflow",
                table: "workflow_activities",
                columns: new[] { "AssignedPersonId", "CompletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_activities_WorkflowId",
                schema: "workflow",
                table: "workflow_activities",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_activity_types_WorkflowTypeId_Key",
                schema: "workflow",
                table: "workflow_activity_types",
                columns: new[] { "WorkflowTypeId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_logs_WorkflowId_LoggedAtUtc",
                schema: "workflow",
                table: "workflow_logs",
                columns: new[] { "WorkflowId", "LoggedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_triggers_IsActive_TriggerKind_EventTypeKey",
                schema: "workflow",
                table: "workflow_triggers",
                columns: new[] { "IsActive", "TriggerKind", "EventTypeKey" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_types_attribute_values",
                schema: "workflow",
                table: "workflow_types",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_types_Slug",
                schema: "workflow",
                table: "workflow_types",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflows_attributes",
                schema: "workflow",
                table: "workflows",
                column: "attributes")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_workflows_Status_NextScheduledProcessAtUtc",
                schema: "workflow",
                table: "workflows",
                columns: new[] { "Status", "NextScheduledProcessAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workflows_WorkflowNumber",
                schema: "workflow",
                table: "workflows",
                column: "WorkflowNumber");

            migrationBuilder.CreateIndex(
                name: "IX_workflows_WorkflowTypeId",
                schema: "workflow",
                table: "workflows",
                column: "WorkflowTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_builder_templates",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_action_form_fields",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_action_form_sections",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_action_types",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_actions",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_logs",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_triggers",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_action_forms",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_activity_types",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_activities",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_types",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflows",
                schema: "workflow");
        }
    }
}
