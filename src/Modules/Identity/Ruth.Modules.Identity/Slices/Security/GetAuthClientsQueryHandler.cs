using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class GetAuthClientsQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetAuthClientsQuery, IReadOnlyList<AuthClientResponse>>
{
    public async Task<Result<IReadOnlyList<AuthClientResponse>>> HandleAsync(
        GetAuthClientsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var clients = await dbContext
            .AuthClients.AsNoTracking()
            .OrderBy(static c => c.Name)
            .Select(static c => new AuthClientResponse(
                c.Id.Value,
                c.Name,
                c.ClientId,
                c.RedirectUri,
                c.PostLogoutRedirectUri,
                c.AllowedScopes,
                c.AllowedClaims,
                c.AllowUserApiAccess,
                c.ScopeApprovalExpirationDays,
                c.IsActive
            ))
            .ToListAsync(cancellationToken);

        return clients;
    }
}
