using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Cms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCmsMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cms");

            migrationBuilder.CreateTable(
                name: "adaptive_messages",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpireDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adaptive_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "block_types",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequiresInteractivity = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    default_settings = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_block_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "blocks",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: true),
                    LayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Zone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    PreHtml = table.Column<string>(type: "text", nullable: true),
                    PostHtml = table.Column<string>(type: "text", nullable: true),
                    CssClass = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    OutputCacheDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    settings = table.Column<string>(type: "jsonb", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_channel_items",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ApprovedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpireDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    AuthorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryTopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    topic_ids = table.Column<string>(type: "jsonb", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_channel_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_channel_types",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DateRangeType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    DisablePriority = table.Column<bool>(type: "boolean", nullable: false),
                    DisableStatus = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeTime = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_channel_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_channels",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentChannelTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IconCssClass = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    EnableRss = table.Column<bool>(type: "boolean", nullable: false),
                    ChannelUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ItemUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TimeToLiveMinutes = table.Column<int>(type: "integer", nullable: false),
                    ItemsManuallyOrdered = table.Column<bool>(type: "boolean", nullable: false),
                    IsStructuredContent = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    child_channel_ids = table.Column<string>(type: "jsonb", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_topics",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ParentTopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "html_contents",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityValue = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpireDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_html_contents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "cms",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.tenant_id, x.message_id, x.consumer_name });
                });

            migrationBuilder.CreateTable(
                name: "interaction_channels",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ChannelType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RetentionDurationDays = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interaction_channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "interaction_components",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InteractionChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ComponentKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    component_data = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interaction_components", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "interaction_sessions",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Browser = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    OperatingSystem = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IpAddressHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastActivityDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interaction_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "interactions",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InteractionComponentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InteractionSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ChannelCustom1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChannelCustom2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OccurredDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    interaction_data = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "layouts",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FileName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    zones = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_layouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "media_accounts",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AccountIdentifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    settings = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "media_elements",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaFolderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ExternalElementKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PlaybackUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    HlsManifestUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CaptionsUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    PublishedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_elements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "media_folders",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalFolderKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContentChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsContentChannelSyncEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_folders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mobile_applications",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    BundleIdentifier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ApiVersion = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    MinimumClientVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LatestClientVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NavigationType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PrimaryColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SecondaryColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    additional_settings = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mobile_applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "cms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "page_short_links",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TargetUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    ClickCount = table.Column<long>(type: "bigint", nullable: false),
                    UtmSource = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    UtmMedium = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    UtmCampaign = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LastClickedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_short_links", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    LayoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentPageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InternalName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PageTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BrowserTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IconCssClass = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    DisplayInNavWhen = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequiresEncryption = table.Column<bool>(type: "boolean", nullable: false),
                    OutputCacheDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "persisted_datasets",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DataSourceKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RefreshIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    ExpireDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MemoryCacheDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    ResultDataJson = table.Column<string>(type: "jsonb", nullable: true),
                    LastRefreshDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TimeToBuildMs = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AllowManualRefresh = table.Column<bool>(type: "boolean", nullable: false),
                    source_configuration = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persisted_datasets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "personal_link_sections",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_link_sections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "personalization_segment_memberships",
                schema: "cms",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AffinityScore = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personalization_segment_memberships", x => new { x.tenant_id, x.SegmentId, x.PersonId });
                });

            migrationBuilder.CreateTable(
                name: "personalization_segments",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SegmentKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TimeToUpdateIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDirty = table.Column<bool>(type: "boolean", nullable: false),
                    filter_spec = table.Column<string>(type: "jsonb", nullable: false),
                    LastEvaluatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CachedMemberCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personalization_segments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "request_filters",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    FilterMode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    rules = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_filters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "signage_applications",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RefreshIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    ThemeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultContentChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    MediaFolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdaptiveMessageKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    slides = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signage_applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sites",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThemeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultPageId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoginPageId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotFoundPageId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    SiteType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    EnablePageViews = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "themes",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CustomCss = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    css_custom_properties = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_themes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "adaptive_message_adaptations",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdaptiveMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    StartDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpireDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ViewSaturationCount = table.Column<int>(type: "integer", nullable: true),
                    ViewSaturationInDays = table.Column<int>(type: "integer", nullable: true),
                    Headline = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BodyMarkup = table.Column<string>(type: "text", nullable: false),
                    CallToActionText = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CallToActionUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    segment_keys = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adaptive_message_adaptations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_adaptive_message_adaptations_adaptive_messages_AdaptiveMess~",
                        column: x => x.AdaptiveMessageId,
                        principalSchema: "cms",
                        principalTable: "adaptive_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_channel_item_associations",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_channel_item_associations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_channel_item_associations_content_channel_items_Par~",
                        column: x => x.ParentItemId,
                        principalSchema: "cms",
                        principalTable: "content_channel_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mobile_deep_links",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MobileApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutePrefix = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetMobilePageId = table.Column<Guid>(type: "uuid", nullable: true),
                    FallbackUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    UsesUniversalLinks = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mobile_deep_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mobile_deep_links_mobile_applications_MobileApplicationId",
                        column: x => x.MobileApplicationId,
                        principalSchema: "cms",
                        principalTable: "mobile_applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mobile_layouts",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MobileApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LayoutKey = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    ShellMarkup = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mobile_layouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mobile_layouts_mobile_applications_MobileApplicationId",
                        column: x => x.MobileApplicationId,
                        principalSchema: "cms",
                        principalTable: "mobile_applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mobile_pages",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MobileApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MobileLayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PageKey = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IconName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ShowInNavigation = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresAuthentication = table.Column<bool>(type: "boolean", nullable: false),
                    ContentChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdaptiveMessageKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BlocksJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mobile_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mobile_pages_mobile_applications_MobileApplicationId",
                        column: x => x.MobileApplicationId,
                        principalSchema: "cms",
                        principalTable: "mobile_applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_contexts",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IdParameter = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_contexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_page_contexts_pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "cms",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_routes",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Route = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_page_routes_pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "cms",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personal_links",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IconName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_personal_links_personal_link_sections_SectionId",
                        column: x => x.SectionId,
                        principalSchema: "cms",
                        principalTable: "personal_link_sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_domains",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_domains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_domains_sites_SiteId",
                        column: x => x.SiteId,
                        principalSchema: "cms",
                        principalTable: "sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adaptive_message_adaptations_AdaptiveMessageId_Order",
                schema: "cms",
                table: "adaptive_message_adaptations",
                columns: new[] { "AdaptiveMessageId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_adaptive_message_adaptations_tenant_id",
                schema: "cms",
                table: "adaptive_message_adaptations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_adaptive_messages_tenant_id_Key",
                schema: "cms",
                table: "adaptive_messages",
                columns: new[] { "tenant_id", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_block_types_tenant_id_Key",
                schema: "cms",
                table: "block_types",
                columns: new[] { "tenant_id", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blocks_attribute_values",
                schema: "cms",
                table: "blocks",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_LayoutId",
                schema: "cms",
                table: "blocks",
                column: "LayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_PageId",
                schema: "cms",
                table: "blocks",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_SiteId",
                schema: "cms",
                table: "blocks",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_tenant_id",
                schema: "cms",
                table: "blocks",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_channel_item_associations_ParentItemId",
                schema: "cms",
                table: "content_channel_item_associations",
                column: "ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_content_channel_item_associations_tenant_id_ParentItemId_Ch~",
                schema: "cms",
                table: "content_channel_item_associations",
                columns: new[] { "tenant_id", "ParentItemId", "ChildItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_channel_items_attribute_values",
                schema: "cms",
                table: "content_channel_items",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_content_channel_items_ContentChannelId_StartDateTimeUtc",
                schema: "cms",
                table: "content_channel_items",
                columns: new[] { "ContentChannelId", "StartDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_content_channel_items_tenant_id_ContentChannelId_Slug",
                schema: "cms",
                table: "content_channel_items",
                columns: new[] { "tenant_id", "ContentChannelId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_channel_types_tenant_id_Key",
                schema: "cms",
                table: "content_channel_types",
                columns: new[] { "tenant_id", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_channels_attribute_values",
                schema: "cms",
                table: "content_channels",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_content_channels_tenant_id_Slug",
                schema: "cms",
                table: "content_channels",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_topics_tenant_id_Slug",
                schema: "cms",
                table: "content_topics",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_html_contents_BlockId_Version",
                schema: "cms",
                table: "html_contents",
                columns: new[] { "BlockId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_html_contents_tenant_id",
                schema: "cms",
                table: "html_contents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_interaction_channels_tenant_id_Key",
                schema: "cms",
                table: "interaction_channels",
                columns: new[] { "tenant_id", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interaction_components_tenant_id_InteractionChannelId_Compo~",
                schema: "cms",
                table: "interaction_components",
                columns: new[] { "tenant_id", "InteractionChannelId", "ComponentKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interaction_sessions_PersonId",
                schema: "cms",
                table: "interaction_sessions",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_interaction_sessions_tenant_id_SessionKey",
                schema: "cms",
                table: "interaction_sessions",
                columns: new[] { "tenant_id", "SessionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interactions_InteractionComponentId_OccurredDateTimeUtc",
                schema: "cms",
                table: "interactions",
                columns: new[] { "InteractionComponentId", "OccurredDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_interactions_PersonId_OccurredDateTimeUtc",
                schema: "cms",
                table: "interactions",
                columns: new[] { "PersonId", "OccurredDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_interactions_tenant_id",
                schema: "cms",
                table: "interactions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_layouts_tenant_id_SiteId_Slug",
                schema: "cms",
                table: "layouts",
                columns: new[] { "tenant_id", "SiteId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_accounts_tenant_id_Slug",
                schema: "cms",
                table: "media_accounts",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_elements_tenant_id_MediaFolderId_ExternalElementKey",
                schema: "cms",
                table: "media_elements",
                columns: new[] { "tenant_id", "MediaFolderId", "ExternalElementKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_folders_tenant_id_MediaAccountId_ExternalFolderKey",
                schema: "cms",
                table: "media_folders",
                columns: new[] { "tenant_id", "MediaAccountId", "ExternalFolderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mobile_applications_tenant_id_Slug",
                schema: "cms",
                table: "mobile_applications",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mobile_deep_links_MobileApplicationId",
                schema: "cms",
                table: "mobile_deep_links",
                column: "MobileApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_deep_links_tenant_id",
                schema: "cms",
                table: "mobile_deep_links",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_layouts_MobileApplicationId",
                schema: "cms",
                table: "mobile_layouts",
                column: "MobileApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_layouts_tenant_id_MobileApplicationId_LayoutKey",
                schema: "cms",
                table: "mobile_layouts",
                columns: new[] { "tenant_id", "MobileApplicationId", "LayoutKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mobile_pages_MobileApplicationId",
                schema: "cms",
                table: "mobile_pages",
                column: "MobileApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_pages_tenant_id_MobileApplicationId_PageKey",
                schema: "cms",
                table: "mobile_pages",
                columns: new[] { "tenant_id", "MobileApplicationId", "PageKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "cms",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_page_contexts_PageId",
                schema: "cms",
                table: "page_contexts",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_page_contexts_tenant_id_PageId_EntityKey",
                schema: "cms",
                table: "page_contexts",
                columns: new[] { "tenant_id", "PageId", "EntityKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_page_routes_PageId",
                schema: "cms",
                table: "page_routes",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_page_routes_Route",
                schema: "cms",
                table: "page_routes",
                column: "Route");

            migrationBuilder.CreateIndex(
                name: "IX_page_routes_tenant_id",
                schema: "cms",
                table: "page_routes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_page_short_links_SiteId",
                schema: "cms",
                table: "page_short_links",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_page_short_links_tenant_id_Token",
                schema: "cms",
                table: "page_short_links",
                columns: new[] { "tenant_id", "Token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pages_attribute_values",
                schema: "cms",
                table: "pages",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_pages_SiteId_Order",
                schema: "cms",
                table: "pages",
                columns: new[] { "SiteId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_pages_tenant_id",
                schema: "cms",
                table: "pages",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_persisted_datasets_tenant_id_AccessKey",
                schema: "cms",
                table: "persisted_datasets",
                columns: new[] { "tenant_id", "AccessKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_personal_link_sections_PersonId",
                schema: "cms",
                table: "personal_link_sections",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_personal_link_sections_tenant_id",
                schema: "cms",
                table: "personal_link_sections",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_personal_links_SectionId",
                schema: "cms",
                table: "personal_links",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_personal_links_tenant_id",
                schema: "cms",
                table: "personal_links",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_personalization_segment_memberships_tenant_id_PersonId",
                schema: "cms",
                table: "personalization_segment_memberships",
                columns: new[] { "tenant_id", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_personalization_segments_tenant_id_SegmentKey",
                schema: "cms",
                table: "personalization_segments",
                columns: new[] { "tenant_id", "SegmentKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_filters_tenant_id_Key",
                schema: "cms",
                table: "request_filters",
                columns: new[] { "tenant_id", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_signage_applications_tenant_id_Slug",
                schema: "cms",
                table: "signage_applications",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_domains_SiteId",
                schema: "cms",
                table: "site_domains",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_site_domains_tenant_id_Domain",
                schema: "cms",
                table: "site_domains",
                columns: new[] { "tenant_id", "Domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sites_attribute_values",
                schema: "cms",
                table: "sites",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_sites_tenant_id_Slug",
                schema: "cms",
                table: "sites",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_themes_tenant_id_Slug",
                schema: "cms",
                table: "themes",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(
                migrationBuilder,
                "cms"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adaptive_message_adaptations",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "block_types",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "blocks",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "content_channel_item_associations",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "content_channel_types",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "content_channels",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "content_topics",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "html_contents",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "interaction_channels",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "interaction_components",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "interaction_sessions",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "interactions",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "layouts",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "media_accounts",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "media_elements",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "media_folders",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "mobile_deep_links",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "mobile_layouts",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "mobile_pages",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "page_contexts",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "page_routes",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "page_short_links",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "persisted_datasets",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "personal_links",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "personalization_segment_memberships",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "personalization_segments",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "request_filters",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "signage_applications",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "site_domains",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "themes",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "adaptive_messages",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "content_channel_items",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "mobile_applications",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "pages",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "personal_link_sections",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "sites",
                schema: "cms");
        }
    }
}
