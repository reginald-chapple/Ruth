using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class GetFinancialStatementTemplateByIdQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetFinancialStatementTemplateByIdQuery, FinancialStatementTemplateResponse>
{
    public async Task<Result<FinancialStatementTemplateResponse>> HandleAsync(
        GetFinancialStatementTemplateByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = FinancialStatementTemplateId.From(query.TemplateId);
        var entity = await dbContext
            .StatementTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return entity is null
            ? FinanceErrors.StatementTemplateNotFound
            : StatementMapper.ToResponse(entity);
    }
}
