using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class GetRestApiKeysQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetRestApiKeysQuery, IReadOnlyList<RestApiKeyResponse>>
{
    public async Task<Result<IReadOnlyList<RestApiKeyResponse>>> HandleAsync(
        GetRestApiKeysQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var source = dbContext.RestApiKeys.AsNoTracking().AsQueryable();
        if (query.UserAccountId.HasValue && query.UserAccountId.Value != Guid.Empty)
        {
            var userId = UserAccountId.From(query.UserAccountId.Value);
            source = source.Where(k => k.UserAccountId == userId);
        }

        var keys = await source
            .OrderByDescending(static k => k.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        var userNames = await dbContext
            .UserAccounts.AsNoTracking()
            .ToDictionaryAsync(static u => u.Id, static u => u.UserName, cancellationToken);

        var results = keys.Select(k => new RestApiKeyResponse(
                k.Id.Value,
                k.Name,
                k.Description,
                k.KeyPrefix,
                k.UserAccountId.Value,
                userNames.GetValueOrDefault(k.UserAccountId) ?? string.Empty,
                k.IsActive,
                k.ExpiresOnUtc,
                k.LastUsedOnUtc,
                k.RevokedOnUtc,
                k.CreatedOnUtc
            ))
            .ToList();

        return results;
    }
}
