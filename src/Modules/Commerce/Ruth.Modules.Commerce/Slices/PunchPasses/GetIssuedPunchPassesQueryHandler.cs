using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Domain.PunchPasses;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Slices.Orders;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.PunchPasses;

internal sealed class GetIssuedPunchPassesQueryHandler(CommerceDbContext dbContext, IClock clock)
    : IQueryHandler<GetIssuedPunchPassesQuery, IReadOnlyList<IssuedPunchPassResponse>>
{
    public async Task<Result<IReadOnlyList<IssuedPunchPassResponse>>> HandleAsync(
        GetIssuedPunchPassesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var nowUtc = clock.UtcNow;
        IQueryable<IssuedPunchPass> q = dbContext
            .IssuedPunchPasses.AsNoTracking()
            .Include(static p => p.LedgerEntries);

        if (query.OwnerPersonId is { } owner && owner != Guid.Empty)
        {
            q = q.Where(p => p.OwnerPersonId == owner);
        }

        if (query.ActiveOnly)
        {
            q = q.Where(p =>
                p.Status == IssuedPunchPassStatus.Active
                && p.RemainingCredits > 0
                && (!p.ExpiresAtUtc.HasValue || p.ExpiresAtUtc > nowUtc)
            );
        }

        var passes = await q.OrderByDescending(static p => p.IssuedAtUtc)
            .ToListAsync(cancellationToken);
        var typeIds = passes.Select(static p => p.PunchPassTypeId).Distinct().ToArray();
        var typeNames = await dbContext
            .PunchPassTypes.AsNoTracking()
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(static t => t.Id, static t => t.Name, cancellationToken);

        return passes
            .Select(p =>
                CommerceOrderMapper.MapIssuedPunchPass(
                    p,
                    typeNames.GetValueOrDefault(p.PunchPassTypeId, "Punch Pass")
                )
            )
            .ToArray();
    }
}
