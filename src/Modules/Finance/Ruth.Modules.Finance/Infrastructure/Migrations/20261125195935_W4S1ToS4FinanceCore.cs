using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W4S1ToS4FinanceCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.CreateTable(
                name: "bank_account_identifiers",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutingAndAccountHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MaskedRoutingNumber = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MaskedAccountNumber = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    BankName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    VerifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_account_identifiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cadence_alert_types",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AlertKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MinHistoricalSamples = table.Column<int>(type: "integer", nullable: false),
                    MultiplierThreshold = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    StandardDeviationSensitivity = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    MinimumAmountThreshold = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CooldownDays = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cadence_alert_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cadence_alerts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BaselineMeanAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BaselineIntervalDays = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ObservedValue = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResolutionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DetectedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cadence_alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "financial_accounts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PublicName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccountKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsTaxDeductible = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultCurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    GlCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CostCenterCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "financial_batches",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ControlAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ControlItemCount = table.Column<int>(type: "integer", nullable: false),
                    BatchStartDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BatchEndDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AccountingSystemCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsAutomated = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AuditLockNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "financial_gateways",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    SupportsCards = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsAch = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    IsTestMode = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    BatchTimeOffsetHours = table.Column<int>(type: "integer", nullable: false),
                    PublicTokenizerKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    settings = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_gateways", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "financial_pledges",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_pledges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "financial_transactions",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    AuthorizedPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: true),
                    SavedPaymentMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecurringScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TenderType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CheckMicrHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "finance",
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
                name: "invoices",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Memo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "finance",
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
                name: "processed_gateway_webhooks",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookEventId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EventKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_gateway_webhooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recurring_schedules",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedPaymentMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    GatewaySubscriptionReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Purpose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextPaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AnchorDayOfMonth = table.Column<int>(type: "integer", nullable: false),
                    LastProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsecutiveFailureCount = table.Column<int>(type: "integer", nullable: false),
                    LastFailureReason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_schedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "saved_payment_methods",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerVaultToken = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    MethodKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CardBrand = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AccountNumberMasked = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiryMonth = table.Column<int>(type: "integer", nullable: true),
                    ExpiryYear = table.Column<int>(type: "integer", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_payment_methods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "financial_transaction_details",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsTaxDeductible = table.Column<bool>(type: "boolean", nullable: false),
                    Summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_transaction_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_financial_transaction_details_financial_transactions_Transa~",
                        column: x => x.TransactionId,
                        principalSchema: "finance",
                        principalTable: "financial_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "financial_transaction_images",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageSide = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    StorageObjectKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OriginalByteLength = table.Column<long>(type: "bigint", nullable: false),
                    Sha256PlaintextHex = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EncryptionNonceBase64 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EncryptionTagBase64 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    KeyIdentifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UploadedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_transaction_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_financial_transaction_images_financial_transactions_Transac~",
                        column: x => x.TransactionId,
                        principalSchema: "finance",
                        principalTable: "financial_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "financial_transaction_refunds",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    GatewayRefundReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ProcessedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_transaction_refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_financial_transaction_refunds_financial_transactions_Transa~",
                        column: x => x.TransactionId,
                        principalSchema: "finance",
                        principalTable: "financial_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_line_items",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    IsTaxDeductible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_line_items_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "finance",
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recurring_schedule_splits",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_schedule_splits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recurring_schedule_splits_recurring_schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalSchema: "finance",
                        principalTable: "recurring_schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bank_account_identifiers_PersonId",
                schema: "finance",
                table: "bank_account_identifiers",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_bank_account_identifiers_RoutingAndAccountHash",
                schema: "finance",
                table: "bank_account_identifiers",
                column: "RoutingAndAccountHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cadence_alerts_PersonId_AlertTypeId_DetectedOnUtc",
                schema: "finance",
                table: "cadence_alerts",
                columns: new[] { "PersonId", "AlertTypeId", "DetectedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cadence_alerts_Status_AlertKind",
                schema: "finance",
                table: "cadence_alerts",
                columns: new[] { "Status", "AlertKind" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_AccountKind_IsActive",
                schema: "finance",
                table: "financial_accounts",
                columns: new[] { "AccountKind", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_attribute_values",
                schema: "finance",
                table: "financial_accounts",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_Code",
                schema: "finance",
                table: "financial_accounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_ParentAccountId",
                schema: "finance",
                table: "financial_accounts",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_batches_Status_BatchStartDateTimeUtc",
                schema: "finance",
                table: "financial_batches",
                columns: new[] { "Status", "BatchStartDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_gateways_ProviderKey_IsActive",
                schema: "finance",
                table: "financial_gateways",
                columns: new[] { "ProviderKey", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_pledges_AccountId_Status",
                schema: "finance",
                table: "financial_pledges",
                columns: new[] { "AccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_pledges_PersonId",
                schema: "finance",
                table: "financial_pledges",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transaction_details_AccountId",
                schema: "finance",
                table: "financial_transaction_details",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transaction_details_InvoiceId",
                schema: "finance",
                table: "financial_transaction_details",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transaction_details_TransactionId",
                schema: "finance",
                table: "financial_transaction_details",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transaction_images_TransactionId",
                schema: "finance",
                table: "financial_transaction_images",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transaction_refunds_TransactionId",
                schema: "finance",
                table: "financial_transaction_refunds",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_AuthorizedPersonId_TransactionDateTi~",
                schema: "finance",
                table: "financial_transactions",
                columns: new[] { "AuthorizedPersonId", "TransactionDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_BatchId",
                schema: "finance",
                table: "financial_transactions",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_items_InvoiceId",
                schema: "finance",
                table: "invoice_line_items",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_InvoiceNumber",
                schema: "finance",
                table: "invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_PersonId_Status",
                schema: "finance",
                table: "invoices",
                columns: new[] { "PersonId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "finance",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_processed_gateway_webhooks_FinancialGatewayId_WebhookEventId",
                schema: "finance",
                table: "processed_gateway_webhooks",
                columns: new[] { "FinancialGatewayId", "WebhookEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_schedule_splits_ScheduleId",
                schema: "finance",
                table: "recurring_schedule_splits",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_schedules_FinancialGatewayId_GatewaySubscriptionR~",
                schema: "finance",
                table: "recurring_schedules",
                columns: new[] { "FinancialGatewayId", "GatewaySubscriptionReference" });

            migrationBuilder.CreateIndex(
                name: "IX_recurring_schedules_PersonId",
                schema: "finance",
                table: "recurring_schedules",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_schedules_Status_NextPaymentDate",
                schema: "finance",
                table: "recurring_schedules",
                columns: new[] { "Status", "NextPaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_saved_payment_methods_FinancialGatewayId_CustomerVaultToken",
                schema: "finance",
                table: "saved_payment_methods",
                columns: new[] { "FinancialGatewayId", "CustomerVaultToken" });

            migrationBuilder.CreateIndex(
                name: "IX_saved_payment_methods_PersonId_IsActive",
                schema: "finance",
                table: "saved_payment_methods",
                columns: new[] { "PersonId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_account_identifiers",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "cadence_alert_types",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "cadence_alerts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "financial_accounts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "financial_batches",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "financial_gateways",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "financial_pledges",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "financial_transaction_details",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "financial_transaction_images",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "financial_transaction_refunds",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "invoice_line_items",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "processed_gateway_webhooks",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "recurring_schedule_splits",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "saved_payment_methods",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "financial_transactions",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "recurring_schedules",
                schema: "finance");
        }
    }
}
