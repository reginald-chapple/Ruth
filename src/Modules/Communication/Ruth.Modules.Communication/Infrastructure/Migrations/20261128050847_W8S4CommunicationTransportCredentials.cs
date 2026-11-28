using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Communication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W8S4CommunicationTransportCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "communication_transport_configs",
                schema: "comms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Channel = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultFromAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    DefaultFromName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    credentials = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_transport_configs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_communication_transport_configs_tenant_id_TransportKey",
                schema: "comms",
                table: "communication_transport_configs",
                columns: new[] { "tenant_id", "TransportKey" },
                unique: true);

            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(migrationBuilder, "comms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "communication_transport_configs",
                schema: "comms");
        }
    }
}
