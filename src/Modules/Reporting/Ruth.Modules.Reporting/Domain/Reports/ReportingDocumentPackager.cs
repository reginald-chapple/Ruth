using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Ruth.Modules.Reporting.Domain.Reports;

/// <summary>
/// Clean-room CSV (RFC 4180), OpenXML Excel (.xlsx), and OpenXML WordprocessingML (.docx)
/// package generators built purely with .NET BCL ZipArchive and XDocument.
/// </summary>
public static class ReportingDocumentPackager
{
    public static byte[] BuildCsv(
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows
    )
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        var sb = new StringBuilder();
        _ = sb.AppendJoin(',', columns.Select(EscapeCsvCell)).AppendLine();

        foreach (var row in rows)
        {
            var cells = columns.Select(col =>
                EscapeCsvCell(row.TryGetValue(col, out var val) ? val : null)
            );
            _ = sb.AppendJoin(',', cells).AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] BuildExcelXlsx(
        string sheetName,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows
    )
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        var safeSheet = SanitizeSheetName(sheetName);
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipXml(zip, "[Content_Types].xml", BuildXlsxContentTypes());
            WriteZipXml(zip, "_rels/.rels", BuildRootRels("xl/workbook.xml", "officeDocument"));
            WriteZipXml(zip, "xl/workbook.xml", BuildWorkbookXml(safeSheet));
            WriteZipXml(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRels());
            WriteZipXml(zip, "xl/worksheets/sheet1.xml", BuildWorksheetXml(columns, rows));
        }

        return ms.ToArray();
    }

    public static byte[] BuildWordDocx(string title, IReadOnlyList<string> renderedSections)
    {
        ArgumentNullException.ThrowIfNull(renderedSections);

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipXml(zip, "[Content_Types].xml", BuildDocxContentTypes());
            WriteZipXml(zip, "_rels/.rels", BuildRootRels("word/document.xml", "officeDocument"));
            WriteZipXml(zip, "word/document.xml", BuildWordDocumentXml(title, renderedSections));
        }

        return ms.ToArray();
    }

    private static readonly char[] _csvSpecialChars = [',', '"', '\n', '\r'];
    private static readonly char[] _formulaPrefixes = ['=', '+', '-', '@'];

    private static string EscapeCsvCell(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Length > 0 && Array.IndexOf(_formulaPrefixes, text[0]) >= 0)
        {
            text = $"'{text}";
        }

        return text.IndexOfAny(_csvSpecialChars) >= 0
            ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : text;
    }

    private static string SanitizeSheetName(string? name)
    {
        var clean = string.IsNullOrWhiteSpace(name) ? "Report" : name.Trim();
        foreach (var bad in new[] { '\\', '/', '?', '*', '[', ']', ':' })
        {
            clean = clean.Replace(bad, ' ');
        }

        return clean.Length > 31 ? clean[..31] : clean;
    }

    private static XDocument BuildXlsxContentTypes()
    {
        XNamespace ct = "http://schemas.openxmlformats.org/package/2006/content-types";
        return new XDocument(
            new XElement(
                ct + "Types",
                new XElement(
                    ct + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute(
                        "ContentType",
                        "application/vnd.openxmlformats-package.relationships+xml"
                    )
                ),
                new XElement(
                    ct + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")
                ),
                new XElement(
                    ct + "Override",
                    new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute(
                        "ContentType",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"
                    )
                ),
                new XElement(
                    ct + "Override",
                    new XAttribute("PartName", "/xl/worksheets/sheet1.xml"),
                    new XAttribute(
                        "ContentType",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"
                    )
                )
            )
        );
    }

    private static XDocument BuildDocxContentTypes()
    {
        XNamespace ct = "http://schemas.openxmlformats.org/package/2006/content-types";
        return new XDocument(
            new XElement(
                ct + "Types",
                new XElement(
                    ct + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute(
                        "ContentType",
                        "application/vnd.openxmlformats-package.relationships+xml"
                    )
                ),
                new XElement(
                    ct + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")
                ),
                new XElement(
                    ct + "Override",
                    new XAttribute("PartName", "/word/document.xml"),
                    new XAttribute(
                        "ContentType",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"
                    )
                )
            )
        );
    }

    private static XDocument BuildRootRels(string target, string relKind)
    {
        XNamespace rel = "http://schemas.openxmlformats.org/package/2006/relationships";
        return new XDocument(
            new XElement(
                rel + "Relationships",
                new XElement(
                    rel + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute(
                        "Type",
                        $"http://schemas.openxmlformats.org/officeDocument/2006/relationships/{relKind}"
                    ),
                    new XAttribute("Target", target)
                )
            )
        );
    }

    private static XDocument BuildWorkbookXml(string sheetName)
    {
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        return new XDocument(
            new XElement(
                main + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", r),
                new XElement(
                    main + "sheets",
                    new XElement(
                        main + "sheet",
                        new XAttribute("name", sheetName),
                        new XAttribute("sheetId", "1"),
                        new XAttribute(r + "id", "rId1")
                    )
                )
            )
        );
    }

    private static XDocument BuildWorkbookRels()
    {
        XNamespace rel = "http://schemas.openxmlformats.org/package/2006/relationships";
        return new XDocument(
            new XElement(
                rel + "Relationships",
                new XElement(
                    rel + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute(
                        "Type",
                        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"
                    ),
                    new XAttribute("Target", "worksheets/sheet1.xml")
                )
            )
        );
    }

    private static XDocument BuildWorksheetXml(
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows
    )
    {
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheetData = new XElement(main + "sheetData");
        sheetData.Add(BuildXlsxRow(main, 1, columns));

        for (var r = 0; r < rows.Count; r++)
        {
            var rowValues = columns
                .Select(c => rows[r].TryGetValue(c, out var v) ? v ?? string.Empty : string.Empty)
                .ToList();
            sheetData.Add(BuildXlsxRow(main, r + 2, rowValues));
        }

        return new XDocument(new XElement(main + "worksheet", sheetData));
    }

    private static XElement BuildXlsxRow(XNamespace main, int rowIndex, IReadOnlyList<string> cells)
    {
        var rowEl = new XElement(
            main + "row",
            new XAttribute("r", rowIndex.ToString(CultureInfo.InvariantCulture))
        );
        for (var c = 0; c < cells.Count; c++)
        {
            var cellRef =
                $"{ToExcelColumnName(c)}{rowIndex.ToString(CultureInfo.InvariantCulture)}";
            rowEl.Add(
                new XElement(
                    main + "c",
                    new XAttribute("r", cellRef),
                    new XAttribute("t", "inlineStr"),
                    new XElement(main + "is", new XElement(main + "t", cells[c]))
                )
            );
        }

        return rowEl;
    }

    private static XDocument BuildWordDocumentXml(
        string title,
        IReadOnlyList<string> renderedSections
    )
    {
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var body = new XElement(w + "body");
        if (!string.IsNullOrWhiteSpace(title))
        {
            body.Add(
                new XElement(
                    w + "p",
                    new XElement(
                        w + "r",
                        new XElement(w + "rPr", new XElement(w + "b")),
                        new XElement(w + "t", title.Trim())
                    )
                )
            );
        }

        foreach (var section in renderedSections)
        {
            foreach (
                var line in section.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            )
            {
                body.Add(
                    new XElement(
                        w + "p",
                        new XElement(
                            w + "r",
                            new XElement(
                                w + "t",
                                new XAttribute(XNamespace.Xml + "space", "preserve"),
                                line
                            )
                        )
                    )
                );
            }
        }

        body.Add(new XElement(w + "sectPr"));
        return new XDocument(
            new XElement(w + "document", new XAttribute(XNamespace.Xmlns + "w", w), body)
        );
    }

    private static string ToExcelColumnName(int zeroBasedIndex)
    {
        var dividend = zeroBasedIndex + 1;
        var name = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            name = (char)('A' + modulo) + name;
            dividend = (dividend - modulo) / 26;
        }

        return name;
    }

    private static void WriteZipXml(ZipArchive zip, string entryName, XDocument doc)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        );
        doc.Save(writer, SaveOptions.DisableFormatting);
    }
}
