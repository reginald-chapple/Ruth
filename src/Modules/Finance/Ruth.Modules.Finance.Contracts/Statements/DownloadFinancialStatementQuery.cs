using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Query to download a generated statement or tax receipt in HTML or PDF format.</summary>
public sealed record DownloadFinancialStatementQuery(
    Guid StatementId,
    StatementDownloadFormat Format = StatementDownloadFormat.Pdf
) : IQuery<FinancialStatementDownloadResponse>;
