using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class GetCurrentUserQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetCurrentUserQuery, AuthenticatedUserResponse>
{
    public async Task<Result<AuthenticatedUserResponse>> HandleAsync(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = UserAccountId.From(query.UserAccountId);
        var account = await dbContext
            .UserAccounts.AsNoTracking()
            .Include(static u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (account is null)
        {
            return IdentityErrors.UserAccountNotFound;
        }

        var roles = await AuthTokenIssuer.LoadActiveRolesAsync(
            dbContext,
            account,
            cancellationToken
        );
        return new AuthenticatedUserResponse(
            account.Id.Value,
            account.UserName,
            account.Email,
            account.DisplayName,
            account.IsConfirmed,
            account.IsLockedOut,
            account.IsActive,
            account.IsPasswordChangeRequired,
            account.LastLoginOnUtc,
            [.. roles.Select(static r => r.Name)],
            [.. roles.Select(static r => r.Id.Value)]
        );
    }
}
