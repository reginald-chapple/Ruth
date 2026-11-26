using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.PaymentMethods;

internal sealed class GetSavedPaymentMethodsQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetSavedPaymentMethodsQuery, IReadOnlyList<SavedPaymentMethodResponse>>
{
    public async Task<Result<IReadOnlyList<SavedPaymentMethodResponse>>> HandleAsync(
        GetSavedPaymentMethodsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var methodsQuery = dbContext.SavedPaymentMethods.AsNoTracking().AsQueryable();
        if (query.PersonId is { } personId && personId != Guid.Empty)
        {
            methodsQuery = methodsQuery.Where(m => m.PersonId == personId);
        }

        if (query.ActiveOnly)
        {
            methodsQuery = methodsQuery.Where(static m => m.IsActive);
        }

        var methods = await methodsQuery
            .OrderByDescending(static m => m.IsDefault)
            .ThenByDescending(static m => m.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        if (methods.Count == 0)
        {
            IReadOnlyList<SavedPaymentMethodResponse> empty = [];
            return Result.Success(empty);
        }

        var gatewayIds = methods.Select(static m => m.FinancialGatewayId).Distinct().ToArray();
        var gatewayNames = await dbContext
            .FinancialGateways.AsNoTracking()
            .Where(g => gatewayIds.Contains(g.Id))
            .ToDictionaryAsync(static g => g.Id, static g => g.Name, cancellationToken);

        IReadOnlyList<SavedPaymentMethodResponse> mapped =
        [
            .. methods.Select(m =>
                SavedPaymentMethodMapper.ToResponse(
                    m,
                    gatewayNames.TryGetValue(m.FinancialGatewayId, out var gName)
                        ? gName
                        : "Gateway"
                )
            ),
        ];

        return Result.Success(mapped);
    }
}
