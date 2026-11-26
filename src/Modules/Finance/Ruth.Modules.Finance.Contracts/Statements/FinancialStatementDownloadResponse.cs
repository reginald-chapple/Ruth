namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Binary download payload for an HTML or PDF financial statement.</summary>
public sealed record FinancialStatementDownloadResponse(
    string FileName,
    string ContentType,
    byte[] ContentBytes
);
