using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.EntitySets;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.EntitySets;

internal sealed class PurgeExpiredEntitySetsCommandHandler(CoreDbContext dbContext, IClock clock)
    : ICommandHandler<PurgeExpiredEntitySetsCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        PurgeExpiredEntitySetsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var nowUtc = clock.UtcNow;
        var expired = await dbContext
            .EntitySets.Where(s => s.ExpireOnUtc != null && s.ExpireOnUtc <= nowUtc)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return Result.Success(0);
        }

        dbContext.EntitySets.RemoveRange(expired);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(expired.Count);
    }
}
