using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeCampusToBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "primary_campus_short_code",
                schema: "identity",
                table: "organization_profiles",
                newName: "primary_branch_short_code");

            migrationBuilder.RenameColumn(
                name: "primary_campus_name",
                schema: "identity",
                table: "organization_profiles",
                newName: "primary_branch_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "primary_branch_short_code",
                schema: "identity",
                table: "organization_profiles",
                newName: "primary_campus_short_code");

            migrationBuilder.RenameColumn(
                name: "primary_branch_name",
                schema: "identity",
                table: "organization_profiles",
                newName: "primary_campus_name");
        }
    }
}
