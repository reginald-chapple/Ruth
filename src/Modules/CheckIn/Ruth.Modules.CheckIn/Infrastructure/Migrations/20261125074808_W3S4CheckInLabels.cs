using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.CheckIn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W3S4CheckInLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "check_in_labels",
                schema: "checkin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LabelKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    layout = table.Column<string>(type: "jsonb", nullable: false),
                    CustomZplOverride = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    PrintOrder = table.Column<int>(type: "integer", nullable: false),
                    Copies = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in_labels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_labels_ConfigurationId",
                schema: "checkin",
                table: "check_in_labels",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_labels_Key",
                schema: "checkin",
                table: "check_in_labels",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_check_in_labels_LabelKind_IsActive",
                schema: "checkin",
                table: "check_in_labels",
                columns: new[] { "LabelKind", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_in_labels",
                schema: "checkin");
        }
    }
}
