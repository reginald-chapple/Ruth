using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Groups.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeBranchAndWeekEndGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CampusId",
                schema: "groups",
                table: "groups",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_groups_CampusId",
                schema: "groups",
                table: "groups",
                newName: "IX_groups_BranchId");

            migrationBuilder.RenameColumn(
                name: "GroupsRequireCampus",
                schema: "groups",
                table: "group_types",
                newName: "GroupsRequireBranch");

            migrationBuilder.RenameColumn(
                name: "AttendanceCountsAsWeekendService",
                schema: "groups",
                table: "group_types",
                newName: "CountsTowardHeadlineAttendance");

            migrationBuilder.RenameColumn(
                name: "CampusId",
                schema: "groups",
                table: "group_historicals",
                newName: "BranchId");

            migrationBuilder.RenameColumn(
                name: "CampusId",
                schema: "groups",
                table: "attendances",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_attendances_CampusId",
                schema: "groups",
                table: "attendances",
                newName: "IX_attendances_BranchId");

            migrationBuilder.RenameColumn(
                name: "SundayDate",
                schema: "groups",
                table: "attendance_occurrences",
                newName: "WeekEndDate");

            migrationBuilder.RenameIndex(
                name: "IX_attendance_occurrences_SundayDate",
                schema: "groups",
                table: "attendance_occurrences",
                newName: "IX_attendance_occurrences_WeekEndDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BranchId",
                schema: "groups",
                table: "groups",
                newName: "CampusId");

            migrationBuilder.RenameIndex(
                name: "IX_groups_BranchId",
                schema: "groups",
                table: "groups",
                newName: "IX_groups_CampusId");

            migrationBuilder.RenameColumn(
                name: "GroupsRequireBranch",
                schema: "groups",
                table: "group_types",
                newName: "GroupsRequireCampus");

            migrationBuilder.RenameColumn(
                name: "CountsTowardHeadlineAttendance",
                schema: "groups",
                table: "group_types",
                newName: "AttendanceCountsAsWeekendService");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                schema: "groups",
                table: "group_historicals",
                newName: "CampusId");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                schema: "groups",
                table: "attendances",
                newName: "CampusId");

            migrationBuilder.RenameIndex(
                name: "IX_attendances_BranchId",
                schema: "groups",
                table: "attendances",
                newName: "IX_attendances_CampusId");

            migrationBuilder.RenameColumn(
                name: "WeekEndDate",
                schema: "groups",
                table: "attendance_occurrences",
                newName: "SundayDate");

            migrationBuilder.RenameIndex(
                name: "IX_attendance_occurrences_WeekEndDate",
                schema: "groups",
                table: "attendance_occurrences",
                newName: "IX_attendance_occurrences_SundayDate");
        }
    }
}
