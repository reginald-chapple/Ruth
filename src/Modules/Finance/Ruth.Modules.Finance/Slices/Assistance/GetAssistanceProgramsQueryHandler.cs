using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal sealed class GetAssistanceProgramsQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetAssistanceProgramsQuery, IReadOnlyList<AssistanceProgramResponse>>
{
    public async Task<Result<IReadOnlyList<AssistanceProgramResponse>>> HandleAsync(
        GetAssistanceProgramsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var efQuery = dbContext.AssistancePrograms.AsNoTracking();
        if (query.Category.HasValue)
        {
            var cat = query.Category.Value;
            efQuery = efQuery.Where(p => p.Category == cat);
        }

        if (query.BranchId.HasValue && query.BranchId.Value != Guid.Empty)
        {
            var branch = query.BranchId.Value;
            efQuery = efQuery.Where(p => p.BranchId == branch);
        }

        if (query.ActiveOnly)
        {
            efQuery = efQuery.Where(static p => p.IsActive);
        }

        var rows = await efQuery.OrderBy(static p => p.Name).ToListAsync(cancellationToken);
        return rows.Select(AssistanceMapper.ToResponse).ToArray();
    }
}
