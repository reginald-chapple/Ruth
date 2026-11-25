using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class VerifyEntityAuthorizationQueryHandler(
    IdentityDbContext dbContext,
    IAclSnapshotCache aclCache
) : IQueryHandler<VerifyEntityAuthorizationQuery, AclVerificationResponse>
{
    public async Task<Result<AclVerificationResponse>> HandleAsync(
        VerifyEntityAuthorizationQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.EntityTypeKey))
        {
            return IdentityErrors.AclEntityTypeRequired;
        }

        if (string.IsNullOrWhiteSpace(query.Action))
        {
            return IdentityErrors.AclActionRequired;
        }

        var subject = await ResolveSubjectAsync(query.UserAccountId, cancellationToken);
        ISecured parent = !string.IsNullOrWhiteSpace(query.ParentEntityTypeKey)
            ? new SecuredEntityRef(
                query.ParentEntityTypeKey.Trim(),
                query.ParentEntityId ?? Guid.Empty,
                GlobalDefaultSecured.Instance
            )
            : GlobalDefaultSecured.Instance;

        var target = new SecuredEntityRef(query.EntityTypeKey.Trim(), query.EntityId, parent);
        var snapshot = await aclCache.GetSnapshotAsync(cancellationToken);
        var trace = AclRuleEvaluator.Evaluate(snapshot, target, query.Action.Trim(), subject);

        return new AclVerificationResponse(
            trace.IsAllowed,
            trace.Source,
            trace.MatchedEntityTypeKey,
            trace.MatchedEntityId,
            trace.MatchedRuleId,
            trace.MatchedOrder,
            trace.Explanation
        );
    }

    private async Task<SecuritySubject> ResolveSubjectAsync(
        Guid? userAccountId,
        CancellationToken cancellationToken
    )
    {
        if (!userAccountId.HasValue || userAccountId.Value == Guid.Empty)
        {
            return SecuritySubject.Anonymous;
        }

        var id = UserAccountId.From(userAccountId.Value);
        var roleIds = await dbContext
            .UserRoles.AsNoTracking()
            .Where(ur => ur.UserAccountId == id)
            .Select(static ur => ur.SecurityRoleId.Value)
            .ToListAsync(cancellationToken);

        return new SecuritySubject(userAccountId.Value, roleIds);
    }
}
