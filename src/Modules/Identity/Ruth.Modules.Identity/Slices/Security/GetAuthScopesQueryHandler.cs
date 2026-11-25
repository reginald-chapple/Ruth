using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class GetAuthScopesQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetAuthScopesQuery, IReadOnlyList<AuthScopeResponse>>
{
    public async Task<Result<IReadOnlyList<AuthScopeResponse>>> HandleAsync(
        GetAuthScopesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var scopes = await dbContext
            .AuthScopes.AsNoTracking()
            .OrderBy(static s => s.Name)
            .Select(static s => new AuthScopeResponse(
                s.Id.Value,
                s.Name,
                s.PublicName,
                s.IsSystem,
                s.IsActive
            ))
            .ToListAsync(cancellationToken);

        return scopes;
    }
}
