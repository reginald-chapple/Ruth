namespace Ruth.Modules.Reporting.Contracts.Reports;

/// <summary>Export file format supported by Reporting.</summary>
public enum ReportExportFormat
{
    /// <summary>RFC 4180 CSV spreadsheet export.</summary>
    Csv = 0,

    /// <summary>OpenXML SpreadsheetML (.xlsx) Excel workbook export.</summary>
    Excel = 1,
}
