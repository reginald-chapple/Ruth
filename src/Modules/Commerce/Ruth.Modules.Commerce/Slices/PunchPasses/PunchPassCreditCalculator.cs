using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Infrastructure;

namespace Ruth.Modules.Commerce.Slices.PunchPasses;

internal sealed class PunchPassCreditCalculator(CommerceDbContext dbContext)
{
    public Task<int> ComputeActivePunchCreditsAsync(
        Guid personId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        return dbContext
            .IssuedPunchPasses.AsNoTracking()
            .Where(p =>
                p.OwnerPersonId == personId
                && p.Status == IssuedPunchPassStatus.Active
                && (!p.ExpiresAtUtc.HasValue || p.ExpiresAtUtc > nowUtc)
            )
            .SumAsync(static p => p.RemainingCredits, cancellationToken);
    }
}
