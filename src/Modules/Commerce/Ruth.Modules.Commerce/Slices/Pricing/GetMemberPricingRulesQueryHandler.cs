using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Slices.Catalog;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Pricing;

internal sealed class GetMemberPricingRulesQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetMemberPricingRulesQuery, IReadOnlyList<MemberPricingRuleResponse>>
{
    public async Task<Result<IReadOnlyList<MemberPricingRuleResponse>>> HandleAsync(
        GetMemberPricingRulesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.MemberPricingRules.AsNoTracking();
        if (query.ActiveOnly)
        {
            q = q.Where(static r => r.IsActive);
        }

        var items = await q.OrderBy(static r => r.Priority)
            .ThenBy(static r => r.Name)
            .ToListAsync(cancellationToken);

        return items.Select(CatalogMapper.MapRule).ToArray();
    }
}
