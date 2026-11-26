using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Search.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSearchWave6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "search");

            migrationBuilder.CreateTable(
                name: "content_collections",
                schema: "search",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    CollectionKey = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EnableRequestFilters = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSegments = table.Column<bool>(type: "boolean", nullable: false),
                    LastIndexDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastIndexItemCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_collections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "search",
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
                schema: "search",
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
                name: "search_documents",
                schema: "search",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentCollectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ModuleName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityTypeKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BodyText = table.Column<string>(type: "text", nullable: true),
                    TagsText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FacetsJson = table.Column<string>(type: "jsonb", nullable: false),
                    UrlPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    SecurityEntityTypeKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SecurityEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IndexedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_collection_sources",
                schema: "search",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentCollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EntityTypeKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OccurrencesToShow = table.Column<int>(type: "integer", nullable: false),
                    CustomBoostWeight = table.Column<double>(type: "double precision", nullable: false),
                    FilterConfigJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_collection_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_collection_sources_content_collections_ContentColle~",
                        column: x => x.ContentCollectionId,
                        principalSchema: "search",
                        principalTable: "content_collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_collection_sources_ContentCollectionId_ProviderKey",
                schema: "search",
                table: "content_collection_sources",
                columns: new[] { "ContentCollectionId", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_collections_CollectionKey",
                schema: "search",
                table: "content_collections",
                column: "CollectionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "search",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_search_documents_ContentCollectionId_EntityTypeKey_EntityId",
                schema: "search",
                table: "search_documents",
                columns: new[] { "ContentCollectionId", "EntityTypeKey", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_search_documents_IsPublic",
                schema: "search",
                table: "search_documents",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_search_documents_ModuleName",
                schema: "search",
                table: "search_documents",
                column: "ModuleName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_collection_sources",
                schema: "search");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "search");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "search");

            migrationBuilder.DropTable(
                name: "search_documents",
                schema: "search");

            migrationBuilder.DropTable(
                name: "content_collections",
                schema: "search");
        }
    }
}
