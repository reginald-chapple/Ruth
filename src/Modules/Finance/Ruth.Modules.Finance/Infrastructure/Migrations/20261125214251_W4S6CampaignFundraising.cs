using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W4S6CampaignFundraising : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fundraising_campaigns",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CampaignGoalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    TeamGoalOverrideAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    DefaultParticipantGoalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    Summary = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    PublicDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AllowTeamGoalOverride = table.Column<bool>(type: "boolean", nullable: false),
                    AllowParticipantGoalOverride = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPublicContributions = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultFinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fundraising_campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "campaign_contribution_credits",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditedParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DonorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DonorDisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DonorPrivacy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DonorComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContributedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_contribution_credits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campaign_contribution_credits_fundraising_campaigns_Campaig~",
                        column: x => x.CampaignId,
                        principalSchema: "finance",
                        principalTable: "fundraising_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign_participants",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CustomGoalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    PersonalMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsFundraisingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublicProfileEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campaign_participants_fundraising_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "finance",
                        principalTable: "fundraising_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_campaign_contribution_credits_CampaignId",
                schema: "finance",
                table: "campaign_contribution_credits",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_contribution_credits_CreditedParticipantId",
                schema: "finance",
                table: "campaign_contribution_credits",
                column: "CreditedParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_contribution_credits_DonorPersonId",
                schema: "finance",
                table: "campaign_contribution_credits",
                column: "DonorPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_contribution_credits_TransactionId",
                schema: "finance",
                table: "campaign_contribution_credits",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_participants_CampaignId_PersonId",
                schema: "finance",
                table: "campaign_participants",
                columns: new[] { "CampaignId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fundraising_campaigns_attribute_values",
                schema: "finance",
                table: "fundraising_campaigns",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_fundraising_campaigns_FinancialAccountId",
                schema: "finance",
                table: "fundraising_campaigns",
                column: "FinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_fundraising_campaigns_GroupId_Status",
                schema: "finance",
                table: "fundraising_campaigns",
                columns: new[] { "GroupId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_fundraising_campaigns_Slug",
                schema: "finance",
                table: "fundraising_campaigns",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaign_contribution_credits",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "campaign_participants",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "fundraising_campaigns",
                schema: "finance");
        }
    }
}
