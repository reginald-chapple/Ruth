using System.Globalization;
using System.Text;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain.Statements;

namespace Ruth.Modules.Finance.Infrastructure.Statements;

/// <summary>Clean-room standards-compliant PDF 1.4 writer generating multi-page printable statements and tax receipts.</summary>
internal static class SimplePdfDocumentWriter
{
    private const int LinesPerPage = 44;

    public static byte[] WriteStatementPdf(
        FinancialStatementTemplate template,
        GeneratedFinancialStatement statement,
        AggregatedStatementLedger ledger
    )
    {
        var lines = BuildLogicalLines(template, statement, ledger);
        var pages = ChunkLines(lines, LinesPerPage);
        return EncodePdfDocument(pages);
    }

    private static List<(bool Bold, string Text)> BuildLogicalLines(
        FinancialStatementTemplate template,
        GeneratedFinancialStatement statement,
        AggregatedStatementLedger ledger
    )
    {
        var rows = new List<(bool Bold, string Text)>
        {
            (true, template.OrganizationDisplayName),
            (
                false,
                string.IsNullOrWhiteSpace(template.OrganizationTaxId)
                    ? $"Document: {statement.StatementNumber}"
                    : $"Tax ID / Reg #: {template.OrganizationTaxId}   |   Document: {statement.StatementNumber}"
            ),
            (false, $"Prepared For ({statement.RecipientScope}): {statement.RecipientDisplayName}"),
            (
                false,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Statement Period: {statement.PeriodStartDate:yyyy-MM-dd} to {statement.PeriodEndDate:yyyy-MM-dd}"
                )
            ),
            (false, string.Empty),
        };

        AppendDeductibleSection(rows, template.StatementKind, statement.CurrencyCode, ledger);
        AppendNonDeductibleSection(rows, template.StatementKind, statement.CurrencyCode, ledger);
        rows.Add((false, string.Empty));
        rows.Add((false, BuildDisclosureLine(template.StatementKind)));
        return rows;
    }

    private static void AppendDeductibleSection(
        List<(bool Bold, string Text)> rows,
        FinancialStatementKind kind,
        string currency,
        AggregatedStatementLedger ledger
    )
    {
        if (
            kind
            is not (
                FinancialStatementKind.TaxDeductibleReceipt
                or FinancialStatementKind.CombinedSeparatedStatement
            )
        )
        {
            return;
        }

        rows.Add((true, "SECTION 1: TAX-DEDUCTIBLE CHARITABLE CONTRIBUTION RECEIPT"));
        foreach (var item in ledger.TaxDeductibleLines)
        {
            rows.Add(
                (
                    false,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"  {item.TransactionDateUtc:yyyy-MM-dd}  {item.AccountName} ({item.AccountCode})  {item.TenderLabel}  {item.NetDeductibleAmount:F2} {currency}"
                    )
                )
            );
        }

        rows.Add(
            (
                true,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"  TOTAL TAX-DEDUCTIBLE CONTRIBUTIONS: {ledger.TaxDeductibleTotal:F2} {currency}"
                )
            )
        );
        rows.Add((false, string.Empty));
    }

    private static void AppendNonDeductibleSection(
        List<(bool Bold, string Text)> rows,
        FinancialStatementKind kind,
        string currency,
        AggregatedStatementLedger ledger
    )
    {
        if (
            kind
            is not (
                FinancialStatementKind.NonDeductibleStatement
                or FinancialStatementKind.CombinedSeparatedStatement
            )
        )
        {
            return;
        }

        rows.Add((true, "SECTION 2: NON-DEDUCTIBLE ACCOUNT STATEMENT (NOT A CHARITABLE RECEIPT)"));
        foreach (var item in ledger.NonDeductibleLines)
        {
            rows.Add(
                (
                    false,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"  {item.TransactionDateUtc:yyyy-MM-dd}  {item.AccountName}  {item.Summary ?? "Fee/Dues"}  {item.NetPaidAmount:F2} {currency}"
                    )
                )
            );
        }

        rows.Add(
            (
                true,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"  TOTAL NON-DEDUCTIBLE PAYMENTS: {ledger.NonDeductiblePaidTotal:F2} {currency}"
                )
            )
        );
        foreach (var inv in ledger.InvoiceSummaries)
        {
            rows.Add(
                (
                    false,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"  Invoice {inv.InvoiceNumber} ({inv.Status}) Due {inv.DueDate:yyyy-MM-dd}: Balance {inv.BalanceDue:F2} {currency}"
                    )
                )
            );
        }
    }

    private static string BuildDisclosureLine(FinancialStatementKind kind)
    {
        return kind switch
        {
            FinancialStatementKind.TaxDeductibleReceipt =>
                "No goods or services were provided in exchange for the tax-deductible contributions above.",
            FinancialStatementKind.NonDeductibleStatement =>
                "Dues, fees, and invoice payments listed above are not deductible as charitable contributions.",
            FinancialStatementKind.CombinedSeparatedStatement =>
                "Only Section 1 items qualify as charitable contributions; Section 2 items are non-deductible dues/fees.",
            _ =>
                "Only Section 1 items qualify as charitable contributions; Section 2 items are non-deductible dues/fees.",
        };
    }

    private static List<List<(bool Bold, string Text)>> ChunkLines(
        List<(bool Bold, string Text)> lines,
        int pageSize
    )
    {
        var pages = new List<List<(bool Bold, string Text)>>();
        for (var i = 0; i < lines.Count; i += pageSize)
        {
            pages.Add(lines.GetRange(i, Math.Min(pageSize, lines.Count - i)));
        }

        if (pages.Count == 0)
        {
            pages.Add([(false, "Empty Statement")]);
        }

        return pages;
    }

    private static byte[] EncodePdfDocument(List<List<(bool Bold, string Text)>> pages)
    {
        var totalObjects = 4 + (pages.Count * 2);
        var offsets = new int[totalObjects + 1];
        using var buffer = new MemoryStream();

        WriteHeaderAndFontObjects(buffer, offsets, pages.Count);
        for (var p = 0; p < pages.Count; p++)
        {
            WriteSinglePageObjects(buffer, offsets, p, pages[p]);
        }

        var xrefPosition = (int)buffer.Position;
        WriteXrefAndTrailer(buffer, offsets, totalObjects, xrefPosition);
        return buffer.ToArray();
    }

    private static void WriteHeaderAndFontObjects(MemoryStream buffer, int[] offsets, int pageCount)
    {
        WriteAscii(buffer, "%PDF-1.4\n");
        offsets[1] = (int)buffer.Position;
        WriteAscii(buffer, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        var kids = string.Join(
            ' ',
            Enumerable
                .Range(0, pageCount)
                .Select(static p =>
                    string.Create(CultureInfo.InvariantCulture, $"{5 + (2 * p)} 0 R")
                )
        );
        offsets[2] = (int)buffer.Position;
        WriteAscii(
            buffer,
            string.Create(
                CultureInfo.InvariantCulture,
                $"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>\nendobj\n"
            )
        );

        offsets[3] = (int)buffer.Position;
        WriteAscii(
            buffer,
            "3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n"
        );
        offsets[4] = (int)buffer.Position;
        WriteAscii(
            buffer,
            "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\nendobj\n"
        );
    }

    private static void WriteSinglePageObjects(
        MemoryStream buffer,
        int[] offsets,
        int pageIndex,
        List<(bool Bold, string Text)> pageLines
    )
    {
        var pageObjId = 5 + (2 * pageIndex);
        var streamObjId = pageObjId + 1;
        var contentBytes = Encoding.ASCII.GetBytes(BuildPageContentStream(pageLines));

        offsets[pageObjId] = (int)buffer.Position;
        WriteAscii(
            buffer,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{pageObjId} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {streamObjId} 0 R >>\nendobj\n"
            )
        );

        offsets[streamObjId] = (int)buffer.Position;
        WriteAscii(
            buffer,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{streamObjId} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n"
            )
        );
        buffer.Write(contentBytes, 0, contentBytes.Length);
        WriteAscii(buffer, "\nendstream\nendobj\n");
    }

    private static string BuildPageContentStream(List<(bool Bold, string Text)> lines)
    {
        var sb = new StringBuilder();
        _ = sb.Append("BT\n42 744 Td\n");
        foreach (var (bold, text) in lines)
        {
            var font = bold ? "/F2 11 Tf" : "/F1 9.5 Tf";
            _ = sb.Append(CultureInfo.InvariantCulture, $"{font} ({EscapePdfString(text)}) Tj\n");
            _ = sb.Append("0 -15 Td\n");
        }

        _ = sb.Append("ET");
        return sb.ToString();
    }

    private static string EscapePdfString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length + 8);
        foreach (var ch in input)
        {
            _ = ch is '\\' or '(' or ')'
                ? sb.Append('\\').Append(ch)
                : sb.Append(ch is >= ' ' and <= '~' ? ch : '?');
        }

        return sb.ToString();
    }

    private static void WriteXrefAndTrailer(
        MemoryStream buffer,
        int[] offsets,
        int totalObjects,
        int xrefPosition
    )
    {
        WriteAscii(
            buffer,
            string.Create(CultureInfo.InvariantCulture, $"xref\n0 {totalObjects + 1}\n")
        );
        WriteAscii(buffer, "0000000000 65535 f \n");
        for (var i = 1; i <= totalObjects; i++)
        {
            WriteAscii(
                buffer,
                string.Create(CultureInfo.InvariantCulture, $"{offsets[i]:D10} 00000 n \n")
            );
        }

        WriteAscii(
            buffer,
            string.Create(
                CultureInfo.InvariantCulture,
                $"trailer\n<< /Size {totalObjects + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF\n"
            )
        );
    }

    private static void WriteAscii(MemoryStream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}
