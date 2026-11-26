using System.Text;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure.Statements;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

public sealed class W4S5StatementsAndAssistanceDomainTests
{
    [Fact]
    public void StatementTemplate_RejectsUnsafeCssAndJavascriptUrls_AndAcceptsPrintCss()
    {
        var unsafeCss = FinancialStatementTemplate.Create(
            new FinancialStatementTemplateSpec(
                "Unsafe",
                FinancialStatementKind.TaxDeductibleReceipt,
                "Civic Org",
                CustomPrintCss: "body { color: black; } </style><script>alert(1)</script>"
            )
        );
        Assert.True(unsafeCss.IsFailure);

        var unsafeLogo = FinancialStatementTemplate.Create(
            new FinancialStatementTemplateSpec(
                "Unsafe Logo",
                FinancialStatementKind.TaxDeductibleReceipt,
                "Civic Org",
                LogoUrl: "javascript:alert(1)"
            )
        );
        Assert.True(unsafeLogo.IsFailure);

        var valid = FinancialStatementTemplate.Create(
            new FinancialStatementTemplateSpec(
                "Clean Print Receipt",
                FinancialStatementKind.CombinedSeparatedStatement,
                "Civic Org",
                OrganizationTaxId: "84-1234567",
                LogoUrl: "https://example.org/logo.png",
                CustomPrintCss: ".stmt-org-name { letter-spacing: 0.02em; }"
            )
        );
        Assert.True(valid.IsSuccess);
    }

    [Fact]
    public void StatementLedgerAggregator_SeparatesDeductibleFromNonDeductible_AndProRatesPartialRefunds()
    {
        var fundId = FinancialAccountId.Create();
        var duesId = FinancialAccountId.Create();
        var catalog = new Dictionary<FinancialAccountId, (string Name, string Code)>
        {
            [fundId] = ("Scholarship Fund", "SCHOLAR"),
            [duesId] = ("Annual Dues", "DUES"),
        };

        var personId = Guid.CreateVersion7();
        var tx = FinancialTransaction
            .Create(
                "USD",
                new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero),
                FinancialTransactionSourceType.Website,
                FinancialTenderType.CreditCard,
                FinancialTransactionStatus.Settled,
                [
                    (fundId, 150.00m, null, true, "Charitable gift"),
                    (duesId, 50.00m, null, false, "Membership dues"),
                ],
                authorizedPersonId: personId
            )
            .Value;

        var refund = tx.RecordRefund(
            50.00m,
            "Partial adjustment",
            "rf_1",
            processedByPersonId: null,
            DateTimeOffset.UtcNow
        );
        Assert.True(refund.IsSuccess);

        var invoice = Invoice
            .Create(
                personId,
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 8, 1),
                "USD",
                [(duesId, "Facility Fee", 1m, 80.00m, false)]
            )
            .Value;

        var receiptOnly = StatementLedgerAggregator.Aggregate(
            FinancialStatementKind.TaxDeductibleReceipt,
            "USD",
            includeOpenInvoices: true,
            includePledgeSummary: true,
            [tx],
            catalog,
            [invoice],
            []
        );
        Assert.Equal(112.50m, receiptOnly.TaxDeductibleTotal);
        Assert.Equal(0m, receiptOnly.NonDeductiblePaidTotal);
        Assert.Empty(receiptOnly.InvoiceSummaries);

        var statementOnly = StatementLedgerAggregator.Aggregate(
            FinancialStatementKind.NonDeductibleStatement,
            "USD",
            includeOpenInvoices: true,
            includePledgeSummary: true,
            [tx],
            catalog,
            [invoice],
            []
        );
        Assert.Equal(0m, statementOnly.TaxDeductibleTotal);
        Assert.Equal(37.50m, statementOnly.NonDeductiblePaidTotal);
        Assert.Equal(80.00m, statementOnly.OpenInvoiceBalanceTotal);

        var combined = StatementLedgerAggregator.Aggregate(
            FinancialStatementKind.CombinedSeparatedStatement,
            "USD",
            includeOpenInvoices: true,
            includePledgeSummary: true,
            [tx],
            catalog,
            [invoice],
            []
        );
        Assert.Equal(112.50m, combined.TaxDeductibleTotal);
        Assert.Equal(37.50m, combined.NonDeductiblePaidTotal);
        Assert.Equal(80.00m, combined.OpenInvoiceBalanceTotal);

        VerifyPdfEncoding(personId, combined);
    }

    private static void VerifyPdfEncoding(Guid personId, AggregatedStatementLedger combined)
    {
        var template = FinancialStatementTemplate
            .Create(
                new FinancialStatementTemplateSpec(
                    "Combined",
                    FinancialStatementKind.CombinedSeparatedStatement,
                    "Civic Org",
                    OrganizationTaxId: "84-9999999"
                )
            )
            .Value;
        var stmt = GeneratedFinancialStatement
            .Create(
                template.Id,
                template.StatementKind,
                FinancialStatementRecipientScope.Individual,
                personId,
                targetHouseholdId: null,
                "Jordan Rivera",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                "USD",
                combined.TaxDeductibleTotal,
                combined.NonDeductiblePaidTotal,
                combined.OpenInvoiceBalanceTotal,
                combined.QualifyingTransactionCount,
                DateTimeOffset.UtcNow
            )
            .Value;

        var pdfBytes = SimplePdfDocumentWriter.WriteStatementPdf(template, stmt, combined);
        var pdfText = Encoding.ASCII.GetString(pdfBytes);
        Assert.StartsWith("%PDF-1.4\n", pdfText, StringComparison.Ordinal);
        Assert.Contains("SECTION 1: TAX-DEDUCTIBLE", pdfText, StringComparison.Ordinal);
        Assert.Contains("SECTION 2: NON-DEDUCTIBLE", pdfText, StringComparison.Ordinal);
        Assert.Contains("xref\n", pdfText, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF\n", pdfText, StringComparison.Ordinal);

        var newPersonId = Guid.CreateVersion7();
        Assert.False(stmt.RepointPerson(Guid.CreateVersion7(), newPersonId));
        Assert.True(stmt.RepointPerson(personId, newPersonId));
        Assert.Equal(newPersonId, stmt.TargetPersonId);
    }

    [Fact]
    public void AssistanceProgramAndCase_EnforceVerifiedDocs_AwardCeilings_Disbursements_AndPersonMerge()
    {
        var fundingId = FinancialAccountId.Create().Value;
        var program = AssistanceProgram
            .Create(
                new AssistanceProgramSpec(
                    "Emergency Utility Aid",
                    "UTIL-AID",
                    AssistanceProgramCategory.EmergencyRelief,
                    fundingId,
                    TotalBudgetAmount: 1000.00m,
                    MaxAwardPerCase: 400.00m,
                    RequiresDocumentation: true
                )
            )
            .Value;

        var applicantId = Guid.CreateVersion7();
        var submitted = AssistanceCase
            .Submit(
                program.Id,
                applicantId,
                350.00m,
                "USD",
                "Winter heating relief",
                DateTimeOffset.UtcNow
            )
            .Value;

        var prematureApprove = submitted.ApplyReviewDecision(
            AssistanceCaseStatus.Approved,
            approvedAmount: 350.00m,
            assignedCaseworkerPersonId: null,
            decisionNotes: "Missing docs",
            DateTimeOffset.UtcNow,
            program.RequiresDocumentation,
            program.MaxAwardPerCase,
            program.RemainingBudgetAmount
        );
        Assert.True(prematureApprove.IsFailure);

        var doc = submitted.AttachDocument(
            "Utility Bill",
            "Invoice",
            binaryFileId: null,
            uploadedByPersonId: null,
            isVerified: true,
            DateTimeOffset.UtcNow
        );
        Assert.True(doc.IsSuccess);

        var overCeiling = submitted.ApplyReviewDecision(
            AssistanceCaseStatus.Approved,
            approvedAmount: 450.00m,
            assignedCaseworkerPersonId: null,
            decisionNotes: "Over ceiling",
            DateTimeOffset.UtcNow,
            program.RequiresDocumentation,
            program.MaxAwardPerCase,
            program.RemainingBudgetAmount
        );
        Assert.True(overCeiling.IsFailure);

        var underReview = submitted.ApplyReviewDecision(
            AssistanceCaseStatus.UnderReview,
            approvedAmount: null,
            assignedCaseworkerPersonId: Guid.CreateVersion7(),
            decisionNotes: "Triaging",
            DateTimeOffset.UtcNow,
            program.RequiresDocumentation,
            program.MaxAwardPerCase,
            program.RemainingBudgetAmount
        );
        Assert.True(underReview.IsSuccess);
        Assert.Equal(AssistanceCaseStatus.UnderReview, submitted.Status);

        var approved = submitted.ApplyReviewDecision(
            AssistanceCaseStatus.Approved,
            approvedAmount: 350.00m,
            assignedCaseworkerPersonId: null,
            decisionNotes: "Verified",
            DateTimeOffset.UtcNow,
            program.RequiresDocumentation,
            program.MaxAwardPerCase,
            program.RemainingBudgetAmount
        );
        Assert.True(approved.IsSuccess);
        Assert.Equal(AssistanceCaseStatus.Approved, submitted.Status);

        VerifyDisbursementsAndMerge(program, submitted, applicantId);
    }

    private static void VerifyDisbursementsAndMerge(
        AssistanceProgram program,
        AssistanceCase submitted,
        Guid applicantId
    )
    {
        Assert.True(program.CommitDisbursement(200.00m).IsSuccess);
        var partial = submitted.RecordDisbursement(
            AssistanceDisbursementMethod.VendorPayment,
            200.00m,
            "Metro Utility Co",
            targetInvoiceId: null,
            referenceNumber: "CHK-101",
            notes: null,
            approvedByPersonId: null,
            DateTimeOffset.UtcNow
        );
        Assert.True(partial.IsSuccess);
        Assert.Equal(AssistanceCaseStatus.PartiallyDisbursed, submitted.Status);

        var overDisburse = submitted.RecordDisbursement(
            AssistanceDisbursementMethod.VendorPayment,
            200.00m,
            "Metro Utility Co",
            targetInvoiceId: null,
            referenceNumber: "CHK-102",
            notes: null,
            approvedByPersonId: null,
            DateTimeOffset.UtcNow
        );
        Assert.True(overDisburse.IsFailure);

        Assert.True(program.CommitDisbursement(150.00m).IsSuccess);
        var finalDisburse = submitted.RecordDisbursement(
            AssistanceDisbursementMethod.VendorPayment,
            150.00m,
            "Metro Utility Co",
            targetInvoiceId: null,
            referenceNumber: "CHK-103",
            notes: null,
            approvedByPersonId: null,
            DateTimeOffset.UtcNow
        );
        Assert.True(finalDisburse.IsSuccess);
        Assert.Equal(AssistanceCaseStatus.Disbursed, submitted.Status);
        Assert.Equal(650.00m, program.RemainingBudgetAmount);

        var survivingId = Guid.CreateVersion7();
        Assert.True(submitted.RepointPerson(applicantId, survivingId));
        Assert.Equal(survivingId, submitted.ApplicantPersonId);
        Assert.True(submitted.Close("Completed", DateTimeOffset.UtcNow).IsSuccess);
        Assert.Equal(AssistanceCaseStatus.Closed, submitted.Status);
    }
}
