using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class GetUserAccountsQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetUserAccountsQuery, IReadOnlyList<UserAccountSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<UserAccountSummaryResponse>>> HandleAsync(
        GetUserAccountsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var accountsQuery = dbContext
            .UserAccounts.AsNoTracking()
            .Include(static u => u.Roles)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var normalized = query.SearchTerm.Trim().ToUpperInvariant();
            accountsQuery = accountsQuery.Where(u =>
                u.NormalizedUserName.Contains(normalized)
                || u.NormalizedEmail.Contains(normalized)
                || u.DisplayName.Contains(query.SearchTerm.Trim())
            );
        }

        var accounts = await accountsQuery
            .OrderBy(static u => u.UserName)
            .Take(250)
            .ToListAsync(cancellationToken);

        var roleLookup = await dbContext
            .SecurityRoles.AsNoTracking()
            .ToDictionaryAsync(static r => r.Id, static r => r.Name, cancellationToken);

        var results = accounts
            .Select(account =>
            {
                var roleIds = account.Roles.Select(static r => r.SecurityRoleId).ToList();
                var roleNames = roleIds
                    .Where(roleLookup.ContainsKey)
                    .Select(id => roleLookup[id])
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new UserAccountSummaryResponse(
                    account.Id.Value,
                    account.UserName,
                    account.Email,
                    account.DisplayName,
                    account.IsConfirmed,
                    account.IsLockedOut,
                    account.IsActive,
                    account.LastLoginOnUtc,
                    account.CreatedOnUtc,
                    roleNames,
                    [.. roleIds.Select(static id => id.Value)]
                );
            })
            .ToList();

        return results;
    }
}
