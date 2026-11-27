using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Identity.Infrastructure;

internal sealed class IdentityTenantSeedContributor(
    IdentityDbContext dbContext,
    IAclSnapshotCache aclSnapshotCache
) : ITenantSeedContributor
{
    public int Order => 10;

    public async Task SeedTenantAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var hasOrganization = await dbContext
            .OrganizationProfiles.AsNoTracking()
            .AnyAsync(cancellationToken);
        if (!hasOrganization)
        {
            return;
        }

        var added = await StageSystemAdministratorsIfMissingAsync(
            dbContext,
            tenantId,
            cancellationToken
        );
        if (added)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await aclSnapshotCache.InvalidateAsync(cancellationToken);
        }
    }

    internal static async Task<bool> StageSystemAdministratorsIfMissingAsync(
        IdentityDbContext dbContext,
        TenantId tenantId,
        CancellationToken cancellationToken
    )
    {
        var adminRoleId = SecurityRole.GetAdministratorsRoleId(tenantId);
        var exists = await dbContext
            .SecurityRoles.AsNoTracking()
            .AnyAsync(role => role.Id == adminRoleId, cancellationToken);
        if (exists)
        {
            return false;
        }

        _ = dbContext.SecurityRoles.Add(SecurityRole.CreateSystemAdministrators(tenantId));
        string[] actions =
        [
            AuthorizationActions.View,
            AuthorizationActions.Edit,
            AuthorizationActions.Delete,
            AuthorizationActions.Administrate,
            AuthorizationActions.Approve,
            AuthorizationActions.Tag,
        ];

        foreach (var action in actions)
        {
            var rule = EntityAclRule.Create(
                GlobalDefaultSecured.TypeKey,
                Guid.Empty,
                action,
                order: 0,
                AclRuleDecision.Allow,
                SpecialRoleType.None,
                adminRoleId
            );
            if (rule.IsSuccess)
            {
                _ = dbContext.EntityAclRules.Add(rule.Value);
            }
        }

        return true;
    }
}
