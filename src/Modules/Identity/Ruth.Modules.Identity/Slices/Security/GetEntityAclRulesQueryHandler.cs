using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class GetEntityAclRulesQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetEntityAclRulesQuery, IReadOnlyList<EntityAclRuleResponse>>
{
    public async Task<Result<IReadOnlyList<EntityAclRuleResponse>>> HandleAsync(
        GetEntityAclRulesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var typeKey = query.EntityTypeKey?.Trim() ?? string.Empty;
        var rulesQuery = dbContext
            .EntityAclRules.AsNoTracking()
            .Where(r => r.EntityTypeKey == typeKey && r.EntityId == query.EntityId);

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            rulesQuery = rulesQuery.Where(r => r.Action == action);
        }

        var rules = await rulesQuery
            .OrderBy(static r => r.Action)
            .ThenBy(static r => r.Order)
            .ToListAsync(cancellationToken);

        var roleNames = await dbContext
            .SecurityRoles.AsNoTracking()
            .ToDictionaryAsync(static r => r.Id, static r => r.Name, cancellationToken);
        var userNames = await dbContext
            .UserAccounts.AsNoTracking()
            .ToDictionaryAsync(static u => u.Id, static u => u.DisplayName, cancellationToken);

        var results = rules
            .Select(r => new EntityAclRuleResponse(
                r.Id.Value,
                r.EntityTypeKey,
                r.EntityId,
                r.Action,
                r.Order,
                r.Decision,
                r.SpecialRole,
                r.SecurityRoleId?.Value,
                r.SecurityRoleId.HasValue
                    ? roleNames.GetValueOrDefault(r.SecurityRoleId.Value)
                    : null,
                r.UserAccountId?.Value,
                r.UserAccountId.HasValue ? userNames.GetValueOrDefault(r.UserAccountId.Value) : null
            ))
            .ToList();

        return results;
    }
}
