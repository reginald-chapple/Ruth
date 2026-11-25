using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class GetExternalAuthProvidersQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetExternalAuthProvidersQuery, IReadOnlyList<ExternalAuthProviderResponse>>
{
    public async Task<Result<IReadOnlyList<ExternalAuthProviderResponse>>> HandleAsync(
        GetExternalAuthProvidersQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var source = dbContext.ExternalAuthProviders.AsNoTracking().AsQueryable();
        if (query.EnabledOnly)
        {
            source = source.Where(static p => p.IsEnabled);
        }

        var providers = await source
            .OrderBy(static p => p.DisplayName)
            .Select(static p => new ExternalAuthProviderResponse(
                p.Id.Value,
                p.ProviderKey,
                p.DisplayName,
                p.Authority,
                p.ClientId,
                p.Scopes,
                p.IsEnabled,
                p.AutoLinkByEmail,
                p.CallbackPath
            ))
            .ToListAsync(cancellationToken);

        return providers;
    }
}
