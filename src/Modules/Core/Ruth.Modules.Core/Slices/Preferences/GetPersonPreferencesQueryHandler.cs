using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Preferences;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Preferences;

internal sealed class GetPersonPreferencesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetPersonPreferencesQuery, IReadOnlyList<PersonPreferenceResponse>>
{
    public async Task<Result<IReadOnlyList<PersonPreferenceResponse>>> HandleAsync(
        GetPersonPreferencesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext
            .PersonPreferences.AsNoTracking()
            .Where(p => p.OwnerKey == query.OwnerKey);

        if (query.EntityTypeId.HasValue)
        {
            var etId = EntityTypeDescriptorId.From(query.EntityTypeId.Value);
            baseQuery = baseQuery.Where(p => p.EntityTypeId == etId);
        }

        if (query.EntityId.HasValue)
        {
            var entityId = query.EntityId.Value;
            baseQuery = baseQuery.Where(p => p.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(query.KeyPrefix))
        {
            var prefix = query.KeyPrefix.Trim();
            baseQuery = baseQuery.Where(p => p.Key.StartsWith(prefix));
        }

        var items = await baseQuery.OrderBy(static p => p.Key).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PersonPreferenceResponse>>([
            .. items.Select(static p => new PersonPreferenceResponse(
                p.Id.Value,
                p.OwnerKey,
                p.Key,
                p.EntityTypeId?.Value,
                p.EntityId,
                p.Value,
                p.IsEnduring,
                p.LastAccessedOnUtc
            )),
        ]);
    }
}
