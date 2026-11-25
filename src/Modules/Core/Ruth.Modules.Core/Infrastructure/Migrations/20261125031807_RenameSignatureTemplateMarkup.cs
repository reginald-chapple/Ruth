using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameSignatureTemplateMarkup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "lava_template",
                schema: "core",
                table: "signature_document_templates",
                newName: "template_markup");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "template_markup",
                schema: "core",
                table: "signature_document_templates",
                newName: "lava_template");
        }
    }
}
