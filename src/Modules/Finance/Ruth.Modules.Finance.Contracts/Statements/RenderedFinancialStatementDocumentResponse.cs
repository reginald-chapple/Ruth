namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Rendered tax receipt or account statement containing separated line details, print HTML, and PDF bytes.</summary>
public sealed record RenderedFinancialStatementDocumentResponse(
    GeneratedFinancialStatementResponse Statement,
    string HtmlContent,
    byte[] PdfBytes,
    IReadOnlyList<StatementContributionLineDto> TaxDeductibleLines,
    IReadOnlyList<StatementNonDeductibleLineDto> NonDeductibleLines,
    IReadOnlyList<StatementInvoiceSummaryDto> InvoiceSummaries,
    IReadOnlyList<StatementPledgeSummaryDto> PledgeSummaries
);
