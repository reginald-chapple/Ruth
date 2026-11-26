using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialFinanceMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.CreateTable(
                name: "assistance_cases",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedCaseworkerPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DecisionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DisbursedAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistance_cases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "assistance_programs",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FundingAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalBudgetAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DisbursedAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaxAwardPerCase = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequiresDocumentation = table.Column<bool>(type: "boolean", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistance_programs", x => x.Id);
                });

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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_pledges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "financial_statement_templates",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    StatementKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OrganizationDisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OrganizationTaxId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HeaderMergeTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FooterMergeTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CustomPrintCss = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IncludePledgeSummary = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeOpenInvoices = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumTotalThreshold = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_statement_templates", x => x.Id);
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fundraising_campaigns",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CampaignGoalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    TeamGoalOverrideAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    DefaultParticipantGoalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    Summary = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    PublicDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AllowTeamGoalOverride = table.Column<bool>(type: "boolean", nullable: false),
                    AllowParticipantGoalOverride = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPublicContributions = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultFinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fundraising_campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "generated_financial_statements",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BatchRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientScope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetHouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PeriodStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TaxDeductibleTotal = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    NonDeductiblePaidTotal = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    OpenInvoiceBalanceTotal = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    QualifyingTransactionCount = table.Column<int>(type: "integer", nullable: false),
                    HtmlStorageKey = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    PdfStorageKey = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_financial_statements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "finance",
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "processed_gateway_webhooks",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookEventId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EventKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_payment_methods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "assistance_disbursements",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisbursementMethod = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PayeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisbursedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistance_disbursements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assistance_disbursements_assistance_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "finance",
                        principalTable: "assistance_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assistance_documents",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DocumentCategory = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    BinaryFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistance_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assistance_documents_assistance_cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "finance",
                        principalTable: "assistance_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    UploadedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    RefundedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "campaign_contribution_credits",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditedParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DonorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    DonorDisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DonorPrivacy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DonorComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContributedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_contribution_credits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campaign_contribution_credits_fundraising_campaigns_Campaig~",
                        column: x => x.CampaignId,
                        principalSchema: "finance",
                        principalTable: "fundraising_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign_participants",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CustomGoalAmount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    PersonalMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsFundraisingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublicProfileEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campaign_participants_fundraising_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "finance",
                        principalTable: "fundraising_campaigns",
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
                    IsTaxDeductible = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "IX_assistance_cases_ApplicantPersonId",
                schema: "finance",
                table: "assistance_cases",
                column: "ApplicantPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_cases_attribute_values",
                schema: "finance",
                table: "assistance_cases",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_cases_ProgramId_Status",
                schema: "finance",
                table: "assistance_cases",
                columns: new[] { "ProgramId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_assistance_cases_tenant_id_CaseNumber",
                schema: "finance",
                table: "assistance_cases",
                columns: new[] { "tenant_id", "CaseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assistance_disbursements_CaseId",
                schema: "finance",
                table: "assistance_disbursements",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_disbursements_TargetInvoiceId",
                schema: "finance",
                table: "assistance_disbursements",
                column: "TargetInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_disbursements_tenant_id",
                schema: "finance",
                table: "assistance_disbursements",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_documents_CaseId",
                schema: "finance",
                table: "assistance_documents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_documents_tenant_id",
                schema: "finance",
                table: "assistance_documents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_programs_Category_IsActive",
                schema: "finance",
                table: "assistance_programs",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_assistance_programs_tenant_id_Code",
                schema: "finance",
                table: "assistance_programs",
                columns: new[] { "tenant_id", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bank_account_identifiers_PersonId",
                schema: "finance",
                table: "bank_account_identifiers",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_bank_account_identifiers_tenant_id_RoutingAndAccountHash",
                schema: "finance",
                table: "bank_account_identifiers",
                columns: new[] { "tenant_id", "RoutingAndAccountHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cadence_alert_types_tenant_id",
                schema: "finance",
                table: "cadence_alert_types",
                column: "tenant_id");

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
                name: "IX_cadence_alerts_tenant_id",
                schema: "finance",
                table: "cadence_alerts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_contribution_credits_CampaignId",
                schema: "finance",
                table: "campaign_contribution_credits",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_contribution_credits_CreditedParticipantId",
                schema: "finance",
                table: "campaign_contribution_credits",
                column: "CreditedParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_contribution_credits_DonorPersonId",
                schema: "finance",
                table: "campaign_contribution_credits",
                column: "DonorPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_contribution_credits_tenant_id",
                schema: "finance",
                table: "campaign_contribution_credits",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_contribution_credits_TransactionId",
                schema: "finance",
                table: "campaign_contribution_credits",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_participants_CampaignId",
                schema: "finance",
                table: "campaign_participants",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_participants_tenant_id_CampaignId_PersonId",
                schema: "finance",
                table: "campaign_participants",
                columns: new[] { "tenant_id", "CampaignId", "PersonId" },
                unique: true);

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
                name: "IX_financial_accounts_ParentAccountId",
                schema: "finance",
                table: "financial_accounts",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_accounts_tenant_id_Code",
                schema: "finance",
                table: "financial_accounts",
                columns: new[] { "tenant_id", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_batches_Status_BatchStartDateTimeUtc",
                schema: "finance",
                table: "financial_batches",
                columns: new[] { "Status", "BatchStartDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_batches_tenant_id",
                schema: "finance",
                table: "financial_batches",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_gateways_ProviderKey_IsActive",
                schema: "finance",
                table: "financial_gateways",
                columns: new[] { "ProviderKey", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_gateways_tenant_id",
                schema: "finance",
                table: "financial_gateways",
                column: "tenant_id");

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
                name: "IX_financial_pledges_tenant_id",
                schema: "finance",
                table: "financial_pledges",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_statement_templates_StatementKind_IsActive",
                schema: "finance",
                table: "financial_statement_templates",
                columns: new[] { "StatementKind", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_statement_templates_tenant_id",
                schema: "finance",
                table: "financial_statement_templates",
                column: "tenant_id");

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
                name: "IX_financial_transaction_details_tenant_id",
                schema: "finance",
                table: "financial_transaction_details",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transaction_details_TransactionId",
                schema: "finance",
                table: "financial_transaction_details",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transaction_images_tenant_id",
                schema: "finance",
                table: "financial_transaction_images",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transaction_images_TransactionId",
                schema: "finance",
                table: "financial_transaction_images",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transaction_refunds_tenant_id",
                schema: "finance",
                table: "financial_transaction_refunds",
                column: "tenant_id");

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
                name: "IX_financial_transactions_tenant_id",
                schema: "finance",
                table: "financial_transactions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_fundraising_campaigns_attribute_values",
                schema: "finance",
                table: "fundraising_campaigns",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_fundraising_campaigns_FinancialAccountId",
                schema: "finance",
                table: "fundraising_campaigns",
                column: "FinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_fundraising_campaigns_GroupId_Status",
                schema: "finance",
                table: "fundraising_campaigns",
                columns: new[] { "GroupId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_fundraising_campaigns_tenant_id_Slug",
                schema: "finance",
                table: "fundraising_campaigns",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_generated_financial_statements_BatchRunId",
                schema: "finance",
                table: "generated_financial_statements",
                column: "BatchRunId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_financial_statements_TargetHouseholdId",
                schema: "finance",
                table: "generated_financial_statements",
                column: "TargetHouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_financial_statements_TargetPersonId",
                schema: "finance",
                table: "generated_financial_statements",
                column: "TargetPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_financial_statements_tenant_id_StatementNumber",
                schema: "finance",
                table: "generated_financial_statements",
                columns: new[] { "tenant_id", "StatementNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_items_InvoiceId",
                schema: "finance",
                table: "invoice_line_items",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_items_tenant_id",
                schema: "finance",
                table: "invoice_line_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_PersonId_Status",
                schema: "finance",
                table: "invoices",
                columns: new[] { "PersonId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_tenant_id_InvoiceNumber",
                schema: "finance",
                table: "invoices",
                columns: new[] { "tenant_id", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "finance",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_processed_gateway_webhooks_tenant_id_FinancialGatewayId_Web~",
                schema: "finance",
                table: "processed_gateway_webhooks",
                columns: new[] { "tenant_id", "FinancialGatewayId", "WebhookEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_schedule_splits_ScheduleId",
                schema: "finance",
                table: "recurring_schedule_splits",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_schedule_splits_tenant_id",
                schema: "finance",
                table: "recurring_schedule_splits",
                column: "tenant_id");

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
                name: "IX_recurring_schedules_tenant_id",
                schema: "finance",
                table: "recurring_schedules",
                column: "tenant_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_saved_payment_methods_tenant_id",
                schema: "finance",
                table: "saved_payment_methods",
                column: "tenant_id");
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(
                migrationBuilder,
                "finance"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistance_disbursements",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "assistance_documents",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "assistance_programs",
                schema: "finance");

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
                name: "campaign_contribution_credits",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "campaign_participants",
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
                name: "financial_statement_templates",
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
                name: "generated_financial_statements",
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
                name: "assistance_cases",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "fundraising_campaigns",
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
