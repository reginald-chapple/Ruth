using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Communication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W3S6BulkCommunicationsAndTwoWaySms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "communication_recipients",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommunicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedDestination = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TransportKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BounceKind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OpenedCount = table.Column<int>(type: "integer", nullable: false),
                    FirstOpenedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClickedCount = table.Column<int>(type: "integer", nullable: false),
                    FirstClickedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastClickedUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StatusNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    merge_fields = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_recipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_responses",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemPhoneNumber = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ParticipantPhoneNumber = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ParticipantPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParticipantDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Direction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MessageBody = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: false),
                    MatchedKeyword = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    RepliedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_responses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_subscription_preferences",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NormalizedDestination = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    GlobalOptOut = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    opted_out_topics = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_subscription_preferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communication_templates",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TopicKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PreheaderText = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    FromName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FromEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    ReplyToEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    SenderSystemPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: true),
                    RazorLayoutKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SmsBodyTemplate = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    PushTitleTemplate = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PushBodyTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    sections = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "communications",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TopicKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsBulk = table.Column<bool>(type: "boolean", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PreheaderText = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    FromName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FromEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    ReplyToEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    SenderSystemPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: true),
                    RazorLayoutKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    HtmlBodyOverride = table.Column<string>(type: "text", nullable: true),
                    SmsBody = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    PushTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PushBody = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PushOpenUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewerPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ScheduledSendAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    sections = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sms_pipelines",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SystemPhoneNumberId = table.Column<Guid>(type: "uuid", nullable: true),
                    PriorityOrder = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_pipelines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sms_actions",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SmsPipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    MatchMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Keyword = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ActionKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReplyTemplate = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    TargetSystemCommunicationKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContinueProcessing = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sms_actions_sms_pipelines_SmsPipelineId",
                        column: x => x.SmsPipelineId,
                        principalSchema: "comms",
                        principalTable: "sms_pipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_communication_recipients_CommunicationId_NormalizedDestinat~",
                schema: "comms",
                table: "communication_recipients",
                columns: new[] { "CommunicationId", "NormalizedDestination" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_recipients_CommunicationId_Status",
                schema: "comms",
                table: "communication_recipients",
                columns: new[] { "CommunicationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_recipients_PersonId",
                schema: "comms",
                table: "communication_recipients",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_recipients_ProviderMessageId",
                schema: "comms",
                table: "communication_recipients",
                column: "ProviderMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_responses_IsRead_Direction_OccurredAtUtc",
                schema: "comms",
                table: "communication_responses",
                columns: new[] { "IsRead", "Direction", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_responses_ParticipantPersonId",
                schema: "comms",
                table: "communication_responses",
                column: "ParticipantPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_responses_SystemPhoneNumberId_ParticipantPhon~",
                schema: "comms",
                table: "communication_responses",
                columns: new[] { "SystemPhoneNumberId", "ParticipantPhoneNumber", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_subscription_preferences_Channel_NormalizedDe~",
                schema: "comms",
                table: "communication_subscription_preferences",
                columns: new[] { "Channel", "NormalizedDestination" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_subscription_preferences_PersonId",
                schema: "comms",
                table: "communication_subscription_preferences",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_templates_Channel_IsActive",
                schema: "comms",
                table: "communication_templates",
                columns: new[] { "Channel", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_templates_Key",
                schema: "comms",
                table: "communication_templates",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communication_templates_SystemGuid",
                schema: "comms",
                table: "communication_templates",
                column: "SystemGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communications_CreatedOnUtc",
                schema: "comms",
                table: "communications",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_communications_Status_ScheduledSendAtUtc",
                schema: "comms",
                table: "communications",
                columns: new[] { "Status", "ScheduledSendAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_actions_SmsPipelineId_Order",
                schema: "comms",
                table: "sms_actions",
                columns: new[] { "SmsPipelineId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_pipelines_IsActive_PriorityOrder",
                schema: "comms",
                table: "sms_pipelines",
                columns: new[] { "IsActive", "PriorityOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_pipelines_SystemGuid",
                schema: "comms",
                table: "sms_pipelines",
                column: "SystemGuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "communication_recipients",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_responses",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_subscription_preferences",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communication_templates",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "communications",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "sms_actions",
                schema: "comms");

            migrationBuilder.DropTable(
                name: "sms_pipelines",
                schema: "comms");
        }
    }
}
