using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.EntitySets;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.EntitySets;

internal sealed class GetEntitySetsQueryHandler(CoreDbContext dbContext, IClock clock)
    : IQueryHandler<GetEntitySetsQuery, IReadOnlyList<EntitySetResponse>>
{
    public async Task<Result<IReadOnlyList<EntitySetResponse>>> HandleAsync(
        GetEntitySetsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var nowUtc = clock.UtcNow;
        var baseQuery = dbContext
            .EntitySets.AsNoTracking()
            .Include(static s => s.Items)
            .AsQueryable();
        if (query.EntityTypeId.HasValue)
        {
            var etId = EntityTypeDescriptorId.From(query.EntityTypeId.Value);
            baseQuery = baseQuery.Where(s => s.EntityTypeId == etId);
        }

        if (!query.IncludeExpired)
        {
            baseQuery = baseQuery.Where(s => s.ExpireOnUtc == null || s.ExpireOnUtc > nowUtc);
        }

        var sets = await baseQuery
            .OrderByDescending(static s => s.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<EntitySetResponse>>([
            .. sets.Select(s => EntitySetMapper.ToResponse(s, nowUtc)),
        ]);
    }
}
