using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.History;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.History;

internal sealed class GetHistoryLogQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetHistoryLogQuery, IReadOnlyList<HistoryLogEntryResponse>>
{
    public async Task<Result<IReadOnlyList<HistoryLogEntryResponse>>> HandleAsync(
        GetHistoryLogQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.HistoryLogEntries.AsNoTracking().AsQueryable();
        if (query.EntityTypeId.HasValue)
        {
            var etId = EntityTypeDescriptorId.From(query.EntityTypeId.Value);
            baseQuery = baseQuery.Where(h => h.EntityTypeId == etId);
        }

        if (query.EntityId.HasValue)
        {
            var entityId = query.EntityId.Value;
            baseQuery = baseQuery.Where(h => h.EntityId == entityId);
        }

        if (query.CategoryId.HasValue)
        {
            var catId = CategoryId.From(query.CategoryId.Value);
            baseQuery = baseQuery.Where(h => h.CategoryId == catId);
        }

        var take = Math.Clamp(query.MaxResults, 1, 500);
        var entries = await baseQuery
            .OrderByDescending(static h => h.OccurredOnUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<HistoryLogEntryResponse>>([
            .. entries.Select(HistoryMapper.ToResponse),
        ]);
    }
}
