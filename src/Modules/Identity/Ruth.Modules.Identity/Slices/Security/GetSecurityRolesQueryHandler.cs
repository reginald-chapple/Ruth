using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class GetSecurityRolesQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetSecurityRolesQuery, IReadOnlyList<SecurityRoleResponse>>
{
    public async Task<Result<IReadOnlyList<SecurityRoleResponse>>> HandleAsync(
        GetSecurityRolesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var source = dbContext.SecurityRoles.AsNoTracking().AsQueryable();
        if (query.ActiveOnly)
        {
            source = source.Where(static r => r.IsActive);
        }

        var roles = await source.OrderBy(static r => r.Name).ToListAsync(cancellationToken);
        var memberCounts = await dbContext
            .UserRoles.AsNoTracking()
            .GroupBy(static ur => ur.SecurityRoleId)
            .Select(static g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(static x => x.RoleId, static x => x.Count, cancellationToken);

        var results = roles
            .Select(r => new SecurityRoleResponse(
                r.Id.Value,
                r.Name,
                r.Description,
                r.IsSystem,
                r.IsActive,
                memberCounts.GetValueOrDefault(r.Id),
                r.CreatedOnUtc
            ))
            .ToList();

        return results;
    }
}
