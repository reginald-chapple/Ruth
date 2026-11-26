using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class GetAvailabilityRulesQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetAvailabilityRulesQuery, IReadOnlyList<AvailabilityRuleResponse>>
{
    public async Task<Result<IReadOnlyList<AvailabilityRuleResponse>>> HandleAsync(
        GetAvailabilityRulesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.AvailabilityRules.AsNoTracking();
        if (query.BookableServiceId is { } sId && sId != Guid.Empty)
        {
            var typed = BookableServiceId.From(sId);
            q = q.Where(r => r.BookableServiceId == typed);
        }

        if (query.RentableResourceId is { } rId && rId != Guid.Empty)
        {
            var typed = RentableResourceId.From(rId);
            q = q.Where(r => r.RentableResourceId == typed);
        }

        if (query.ProviderPersonId is { } pId && pId != Guid.Empty)
        {
            q = q.Where(r => r.ProviderPersonId == pId);
        }

        var list = await q.OrderBy(static r => r.Name).ToListAsync(cancellationToken);
        return list.Select(BookingsMapper.MapRule).ToArray();
    }
}
