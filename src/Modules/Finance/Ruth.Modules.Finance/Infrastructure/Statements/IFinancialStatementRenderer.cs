using Ruth.Modules.Finance.Domain.Statements;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Infrastructure.Statements;

internal interface IFinancialStatementRenderer
{
    Task<Result<(string HtmlContent, byte[] PdfBytes)>> RenderAsync(
        FinancialStatementTemplate template,
        GeneratedFinancialStatement statement,
        AggregatedStatementLedger ledger,
        CancellationToken cancellationToken
    );
}
