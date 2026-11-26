using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Reporting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialReportingMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "data_view_persisted_values",
                schema: "reporting",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataViewId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersistedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_view_persisted_values", x => new { x.tenant_id, x.DataViewId, x.EntityId });
                });

            migrationBuilder.CreateTable(
                name: "data_views",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityTypeKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TransformKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    PersistedScheduleIntervalMinutes = table.Column<int>(type: "integer", nullable: true),
                    PersistedLastRefreshDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PersistedLastRunDurationMilliseconds = table.Column<int>(type: "integer", nullable: true),
                    PersistedResultCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_views", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dim_branch",
                schema: "reporting",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ShortCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RefreshedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dim_branch", x => new { x.tenant_id, x.BranchId });
                });

            migrationBuilder.CreateTable(
                name: "dim_date",
                schema: "reporting",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateKey = table.Column<int>(type: "integer", nullable: false),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Quarter = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    MonthLabel = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    IsoWeek = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dim_date", x => new { x.tenant_id, x.DateKey });
                });

            migrationBuilder.CreateTable(
                name: "dim_metric",
                schema: "reporting",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    UnitLabel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SourceType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    RefreshedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dim_metric", x => new { x.tenant_id, x.MetricId });
                });

            migrationBuilder.CreateTable(
                name: "fact_attendance_weekly",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekEndDateKey = table.Column<int>(type: "integer", nullable: false),
                    WeekEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    AttendedCount = table.Column<int>(type: "integer", nullable: false),
                    RefreshedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fact_attendance_weekly", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fact_metric_daily",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateKey = table.Column<int>(type: "integer", nullable: false),
                    MetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartitionEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartitionLabel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    MeasureSum = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GoalSum = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    RefreshedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fact_metric_daily", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fact_revenue_monthly",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthDateKey = table.Column<int>(type: "integer", nullable: false),
                    MonthLabel = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaxDeductibleGiving = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NonDeductibleDuesAndFees = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false),
                    RefreshedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fact_revenue_monthly", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "reporting",
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
                name: "merge_templates",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TemplateBody = table.Column<string>(type: "text", nullable: false),
                    HeaderMarkup = table.Column<string>(type: "text", nullable: true),
                    FooterMarkup = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merge_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "metric_categories",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "metric_values",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricValueType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    YValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MetricValueDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    XValue = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_values", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "metrics",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Subtitle = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    MetricKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsCumulative = table.Column<bool>(type: "boolean", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    DataViewId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceQueryKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ScheduleCronExpression = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    YAxisLabel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    UnitLabel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastRunDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "reporting",
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
                name: "reports",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EntityTypeKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DataViewId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    FetchTop = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "data_view_filters",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataViewId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentFilterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpressionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RuleKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Operator = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SecondaryValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_view_filters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_data_view_filters_data_views_DataViewId",
                        column: x => x.DataViewId,
                        principalSchema: "reporting",
                        principalTable: "data_views",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metric_value_partitions",
                schema: "reporting",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricPartitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityLabel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_value_partitions", x => new { x.tenant_id, x.MetricValueId, x.MetricPartitionId });
                    table.ForeignKey(
                        name: "FK_metric_value_partitions_metric_values_MetricValueId",
                        column: x => x.MetricValueId,
                        principalSchema: "reporting",
                        principalTable: "metric_values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metric_partitions",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EntityTypeKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_partitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_metric_partitions_metrics_MetricId",
                        column: x => x.MetricId,
                        principalSchema: "reporting",
                        principalTable: "metrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_fields",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldSelectKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ColumnHeaderText = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    ColumnOrder = table.Column<int>(type: "integer", nullable: false),
                    SelectionConfigJson = table.Column<string>(type: "jsonb", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
                    SortDirection = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ShowInGrid = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_fields_reports_ReportId",
                        column: x => x.ReportId,
                        principalSchema: "reporting",
                        principalTable: "reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_data_view_filters_DataViewId_ParentFilterId_Order",
                schema: "reporting",
                table: "data_view_filters",
                columns: new[] { "DataViewId", "ParentFilterId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_data_view_filters_tenant_id",
                schema: "reporting",
                table: "data_view_filters",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_data_view_persisted_values_EntityId",
                schema: "reporting",
                table: "data_view_persisted_values",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_data_views_EntityTypeKey",
                schema: "reporting",
                table: "data_views",
                column: "EntityTypeKey");

            migrationBuilder.CreateIndex(
                name: "IX_data_views_tenant_id",
                schema: "reporting",
                table: "data_views",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_dim_date_tenant_id_CalendarDate",
                schema: "reporting",
                table: "dim_date",
                columns: new[] { "tenant_id", "CalendarDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fact_attendance_weekly_tenant_id",
                schema: "reporting",
                table: "fact_attendance_weekly",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_fact_attendance_weekly_WeekEndDateKey_BranchId",
                schema: "reporting",
                table: "fact_attendance_weekly",
                columns: new[] { "WeekEndDateKey", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_fact_metric_daily_DateKey_MetricId",
                schema: "reporting",
                table: "fact_metric_daily",
                columns: new[] { "DateKey", "MetricId" });

            migrationBuilder.CreateIndex(
                name: "IX_fact_metric_daily_tenant_id",
                schema: "reporting",
                table: "fact_metric_daily",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_fact_revenue_monthly_MonthDateKey_BranchId",
                schema: "reporting",
                table: "fact_revenue_monthly",
                columns: new[] { "MonthDateKey", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_fact_revenue_monthly_tenant_id",
                schema: "reporting",
                table: "fact_revenue_monthly",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_merge_templates_tenant_id",
                schema: "reporting",
                table: "merge_templates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_categories_tenant_id",
                schema: "reporting",
                table: "metric_categories",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_partitions_MetricId_Order",
                schema: "reporting",
                table: "metric_partitions",
                columns: new[] { "MetricId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_metric_partitions_tenant_id",
                schema: "reporting",
                table: "metric_partitions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_value_partitions_EntityId",
                schema: "reporting",
                table: "metric_value_partitions",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_metric_value_partitions_MetricValueId",
                schema: "reporting",
                table: "metric_value_partitions",
                column: "MetricValueId");

            migrationBuilder.CreateIndex(
                name: "IX_metric_values_MetricId_MetricValueDateTimeUtc",
                schema: "reporting",
                table: "metric_values",
                columns: new[] { "MetricId", "MetricValueDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_metric_values_tenant_id",
                schema: "reporting",
                table: "metric_values",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_metrics_tenant_id_MetricKey",
                schema: "reporting",
                table: "metrics",
                columns: new[] { "tenant_id", "MetricKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "reporting",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_report_fields_ReportId_ColumnOrder",
                schema: "reporting",
                table: "report_fields",
                columns: new[] { "ReportId", "ColumnOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_report_fields_tenant_id",
                schema: "reporting",
                table: "report_fields",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_EntityTypeKey",
                schema: "reporting",
                table: "reports",
                column: "EntityTypeKey");

            migrationBuilder.CreateIndex(
                name: "IX_reports_tenant_id",
                schema: "reporting",
                table: "reports",
                column: "tenant_id");
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(
                migrationBuilder,
                "reporting"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_view_filters",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "data_view_persisted_values",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "dim_branch",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "dim_date",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "dim_metric",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "fact_attendance_weekly",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "fact_metric_daily",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "fact_revenue_monthly",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "merge_templates",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "metric_categories",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "metric_partitions",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "metric_value_partitions",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "report_fields",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "data_views",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "metrics",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "metric_values",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "reporting");
        }
    }
}
