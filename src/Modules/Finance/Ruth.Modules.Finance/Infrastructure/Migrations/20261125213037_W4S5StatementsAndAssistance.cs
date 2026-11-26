using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class W4S5StatementsAndAssistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistance_programs", x => x.Id);
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
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_statement_templates", x => x.Id);
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
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generated_financial_statements", x => x.Id);
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
                    DisbursedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_assistance_cases_CaseNumber",
                schema: "finance",
                table: "assistance_cases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assistance_cases_ProgramId_Status",
                schema: "finance",
                table: "assistance_cases",
                columns: new[] { "ProgramId", "Status" });

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
                name: "IX_assistance_documents_CaseId",
                schema: "finance",
                table: "assistance_documents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_programs_Category_IsActive",
                schema: "finance",
                table: "assistance_programs",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_assistance_programs_Code",
                schema: "finance",
                table: "assistance_programs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_statement_templates_StatementKind_IsActive",
                schema: "finance",
                table: "financial_statement_templates",
                columns: new[] { "StatementKind", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_generated_financial_statements_BatchRunId",
                schema: "finance",
                table: "generated_financial_statements",
                column: "BatchRunId");

            migrationBuilder.CreateIndex(
                name: "IX_generated_financial_statements_StatementNumber",
                schema: "finance",
                table: "generated_financial_statements",
                column: "StatementNumber",
                unique: true);

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
                name: "financial_statement_templates",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "generated_financial_statements",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "assistance_cases",
                schema: "finance");
        }
    }
}
