using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.CadenceAlerts;

internal sealed class GetCadenceAlertTypesQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetCadenceAlertTypesQuery, IReadOnlyList<TransactionCadenceAlertTypeResponse>>
{
    public async Task<Result<IReadOnlyList<TransactionCadenceAlertTypeResponse>>> HandleAsync(
        GetCadenceAlertTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var typesQuery = dbContext.CadenceAlertTypes.AsNoTracking().AsQueryable();
        if (query.ActiveOnly)
        {
            typesQuery = typesQuery.Where(static t => t.IsActive);
        }

        var rows = await typesQuery.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        IReadOnlyList<TransactionCadenceAlertTypeResponse> mapped =
        [
            .. rows.Select(CadenceAlertMapper.ToTypeResponse),
        ];
        return Result.Success(mapped);
    }
}
