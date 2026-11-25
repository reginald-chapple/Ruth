using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeCampusAndCleanRoomCrm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LegacyPersonAliasGuid",
                schema: "crm",
                table: "person_redirects",
                newName: "ExternalRecordGuid");

            migrationBuilder.RenameIndex(
                name: "IX_person_redirects_LegacyPersonAliasGuid",
                schema: "crm",
                table: "person_redirects",
                newName: "IX_person_redirects_ExternalRecordGuid");

            migrationBuilder.RenameColumn(
                name: "PrimaryCampusId",
                schema: "crm",
                table: "people",
                newName: "PrimaryBranchId");

            migrationBuilder.RenameColumn(
                name: "GivingHouseholdId",
                schema: "crm",
                table: "people",
                newName: "BillingHouseholdId");

            migrationBuilder.RenameIndex(
                name: "IX_people_PrimaryCampusId",
                schema: "crm",
                table: "people",
                newName: "IX_people_PrimaryBranchId");

            migrationBuilder.RenameColumn(
                name: "CombineGivingByDefault",
                schema: "crm",
                table: "households",
                newName: "CombineContributionsByDefault");

            migrationBuilder.RenameColumn(
                name: "CampusId",
                schema: "crm",
                table: "households",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_households_CampusId",
                schema: "crm",
                table: "households",
                newName: "IX_households_BranchId");

            migrationBuilder.RenameColumn(
                name: "IsCombinedGivingMember",
                schema: "crm",
                table: "household_members",
                newName: "IsCombinedContributionMember");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimaryContact",
                schema: "crm",
                table: "household_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrimaryContact",
                schema: "crm",
                table: "household_members");

            migrationBuilder.RenameColumn(
                name: "ExternalRecordGuid",
                schema: "crm",
                table: "person_redirects",
                newName: "LegacyPersonAliasGuid");

            migrationBuilder.RenameIndex(
                name: "IX_person_redirects_ExternalRecordGuid",
                schema: "crm",
                table: "person_redirects",
                newName: "IX_person_redirects_LegacyPersonAliasGuid");

            migrationBuilder.RenameColumn(
                name: "PrimaryBranchId",
                schema: "crm",
                table: "people",
                newName: "PrimaryCampusId");

            migrationBuilder.RenameColumn(
                name: "BillingHouseholdId",
                schema: "crm",
                table: "people",
                newName: "GivingHouseholdId");

            migrationBuilder.RenameIndex(
                name: "IX_people_PrimaryBranchId",
                schema: "crm",
                table: "people",
                newName: "IX_people_PrimaryCampusId");

            migrationBuilder.RenameColumn(
                name: "CombineContributionsByDefault",
                schema: "crm",
                table: "households",
                newName: "CombineGivingByDefault");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                schema: "crm",
                table: "households",
                newName: "CampusId");

            migrationBuilder.RenameIndex(
                name: "IX_households_BranchId",
                schema: "crm",
                table: "households",
                newName: "IX_households_CampusId");

            migrationBuilder.RenameColumn(
                name: "IsCombinedContributionMember",
                schema: "crm",
                table: "household_members",
                newName: "IsCombinedGivingMember");
        }
    }
}
