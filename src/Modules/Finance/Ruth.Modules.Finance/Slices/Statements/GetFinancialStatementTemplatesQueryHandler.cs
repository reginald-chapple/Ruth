using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class GetFinancialStatementTemplatesQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<
        GetFinancialStatementTemplatesQuery,
        IReadOnlyList<FinancialStatementTemplateResponse>
    >
{
    public async Task<Result<IReadOnlyList<FinancialStatementTemplateResponse>>> HandleAsync(
        GetFinancialStatementTemplatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var efQuery = dbContext.StatementTemplates.AsNoTracking();
        if (query.StatementKind.HasValue)
        {
            var kind = query.StatementKind.Value;
            efQuery = efQuery.Where(t => t.StatementKind == kind);
        }

        if (query.ActiveOnly)
        {
            efQuery = efQuery.Where(static t => t.IsActive);
        }

        var rows = await efQuery
            .OrderByDescending(static t => t.IsDefault)
            .ThenBy(static t => t.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(StatementMapper.ToResponse).ToArray();
    }
}
