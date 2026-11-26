using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class GetGeneratedFinancialStatementsQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<
        GetGeneratedFinancialStatementsQuery,
        IReadOnlyList<GeneratedFinancialStatementResponse>
    >
{
    public async Task<Result<IReadOnlyList<GeneratedFinancialStatementResponse>>> HandleAsync(
        GetGeneratedFinancialStatementsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var efQuery = dbContext.GeneratedStatements.AsNoTracking();
        if (query.TargetPersonId.HasValue && query.TargetPersonId.Value != Guid.Empty)
        {
            var pId = query.TargetPersonId.Value;
            efQuery = efQuery.Where(s => s.TargetPersonId == pId);
        }

        if (query.TargetHouseholdId.HasValue && query.TargetHouseholdId.Value != Guid.Empty)
        {
            var hhId = query.TargetHouseholdId.Value;
            efQuery = efQuery.Where(s => s.TargetHouseholdId == hhId);
        }

        if (query.BatchRunId.HasValue && query.BatchRunId.Value != Guid.Empty)
        {
            var bId = query.BatchRunId.Value;
            efQuery = efQuery.Where(s => s.BatchRunId == bId);
        }

        if (query.StatementKind.HasValue)
        {
            var kind = query.StatementKind.Value;
            efQuery = efQuery.Where(s => s.StatementKind == kind);
        }

        var limit = Math.Clamp(query.Limit, 1, 500);
        var statements = await efQuery
            .OrderByDescending(static s => s.GeneratedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var templateNames = await dbContext
            .StatementTemplates.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, static t => t.Name, cancellationToken);

        return statements
            .Select(s =>
                StatementMapper.ToResponse(
                    s,
                    templateNames.GetValueOrDefault(s.TemplateId, "Statement Template")
                )
            )
            .ToArray();
    }
}
