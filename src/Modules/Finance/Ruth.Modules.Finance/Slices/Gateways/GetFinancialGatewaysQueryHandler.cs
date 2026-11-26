using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Gateways;

internal sealed class GetFinancialGatewaysQueryHandler(
    FinanceDbContext dbContext,
    IPaymentGatewayRegistry registry
) : IQueryHandler<GetFinancialGatewaysQuery, IReadOnlyList<FinancialGatewayResponse>>
{
    public async Task<Result<IReadOnlyList<FinancialGatewayResponse>>> HandleAsync(
        GetFinancialGatewaysQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var gatewaysQuery = dbContext.FinancialGateways.AsNoTracking().AsQueryable();
        if (query.ActiveOnly)
        {
            gatewaysQuery = gatewaysQuery.Where(static g => g.IsActive);
        }

        var gateways = await gatewaysQuery
            .OrderBy(static g => g.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyList<FinancialGatewayResponse> mapped =
        [
            .. gateways.Select(g => FinancialGatewayMapper.ToResponse(g, registry)),
        ];
        return Result.Success(mapped);
    }
}
