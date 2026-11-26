namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Supported output formats when downloading a generated financial statement or tax receipt.</summary>
public enum StatementDownloadFormat
{
    /// <summary>Standards-compliant HTML5 document with print stylesheet.</summary>
    Html = 0,

    /// <summary>Portable Document Format (PDF 1.4) binary document.</summary>
    Pdf = 1,
}
