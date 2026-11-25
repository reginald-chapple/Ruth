using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class SaveEntityAclRulesCommandHandler(
    IdentityDbContext dbContext,
    IAclSnapshotCache aclCache,
    IClock clock
) : ICommandHandler<SaveEntityAclRulesCommand, IReadOnlyList<EntityAclRuleResponse>>
{
    public async Task<Result<IReadOnlyList<EntityAclRuleResponse>>> HandleAsync(
        SaveEntityAclRulesCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.EntityTypeKey))
        {
            return IdentityErrors.AclEntityTypeRequired;
        }

        if (string.IsNullOrWhiteSpace(command.Action))
        {
            return IdentityErrors.AclActionRequired;
        }

        var typeKey = command.EntityTypeKey.Trim();
        var action = command.Action.Trim();
        var utcNow = clock.UtcNow;
        var changedBy =
            command.ChangedByUserId.HasValue && command.ChangedByUserId.Value != Guid.Empty
                ? UserAccountId.From(command.ChangedByUserId.Value)
                : (UserAccountId?)null;

        var newRulesResult = BuildNewRules(typeKey, command.EntityId, action, command.Rules);
        if (newRulesResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<EntityAclRuleResponse>>(newRulesResult.Errors);
        }

        var existingRules = await dbContext
            .EntityAclRules.Where(r =>
                r.EntityTypeKey == typeKey && r.EntityId == command.EntityId && r.Action == action
            )
            .OrderBy(static r => r.Order)
            .ToListAsync(cancellationToken);

        RecordAuditTrail(
            typeKey,
            command.EntityId,
            action,
            existingRules,
            newRulesResult.Value,
            utcNow,
            changedBy
        );

        dbContext.EntityAclRules.RemoveRange(existingRules);
        dbContext.EntityAclRules.AddRange(newRulesResult.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await aclCache.InvalidateAsync(cancellationToken);

        var saved = await LoadResponsesAsync(newRulesResult.Value, cancellationToken);
        return Result.Success(saved);
    }

    private static Result<List<EntityAclRule>> BuildNewRules(
        string typeKey,
        Guid entityId,
        string action,
        IReadOnlyList<AclRuleInput> inputs
    )
    {
        var list = new List<EntityAclRule>(inputs.Count);
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            var created = EntityAclRule.Create(
                typeKey,
                entityId,
                action,
                order: i,
                input.Decision,
                input.SpecialRole,
                input.SecurityRoleId.HasValue
                    ? SecurityRoleId.From(input.SecurityRoleId.Value)
                    : null,
                input.UserAccountId.HasValue ? UserAccountId.From(input.UserAccountId.Value) : null
            );
            if (created.IsFailure)
            {
                return Result.Failure<List<EntityAclRule>>(created.Errors);
            }

            list.Add(created.Value);
        }

        return list;
    }

    private void RecordAuditTrail(
        string typeKey,
        Guid entityId,
        string action,
        List<EntityAclRule> existingRules,
        List<EntityAclRule> newRules,
        DateTimeOffset utcNow,
        UserAccountId? changedBy
    )
    {
        var existingByPrincipal = existingRules
            .GroupBy(GetPrincipalKey)
            .ToDictionary(static g => g.Key, static g => g.First());
        var newByPrincipal = newRules
            .GroupBy(GetPrincipalKey)
            .ToDictionary(static g => g.Key, static g => g.First());

        foreach (var oldRule in existingRules)
        {
            RecordExistingRuleDiff(
                typeKey,
                entityId,
                action,
                oldRule,
                newByPrincipal,
                utcNow,
                changedBy
            );
        }

        foreach (
            var added in newRules.Where(r => !existingByPrincipal.ContainsKey(GetPrincipalKey(r)))
        )
        {
            AddAuditEntry(
                typeKey,
                entityId,
                action,
                AclAuditChangeType.Added,
                utcNow,
                changedBy,
                preDecision: null,
                postDecision: added.Decision,
                preOrder: null,
                postOrder: added.Order,
                added
            );
        }
    }

    private void RecordExistingRuleDiff(
        string typeKey,
        Guid entityId,
        string action,
        EntityAclRule oldRule,
        Dictionary<(SpecialRoleType, Guid?, Guid?), EntityAclRule> newByPrincipal,
        DateTimeOffset utcNow,
        UserAccountId? changedBy
    )
    {
        if (!newByPrincipal.TryGetValue(GetPrincipalKey(oldRule), out var replacement))
        {
            AddAuditEntry(
                typeKey,
                entityId,
                action,
                AclAuditChangeType.Deleted,
                utcNow,
                changedBy,
                preDecision: oldRule.Decision,
                postDecision: null,
                preOrder: oldRule.Order,
                postOrder: null,
                oldRule
            );
            return;
        }

        if (oldRule.Decision == replacement.Decision && oldRule.Order == replacement.Order)
        {
            return;
        }

        var changeType =
            oldRule.Decision != replacement.Decision
                ? AclAuditChangeType.Modified
                : AclAuditChangeType.Reordered;
        AddAuditEntry(
            typeKey,
            entityId,
            action,
            changeType,
            utcNow,
            changedBy,
            preDecision: oldRule.Decision,
            postDecision: replacement.Decision,
            preOrder: oldRule.Order,
            postOrder: replacement.Order,
            replacement
        );
    }

    private void AddAuditEntry(
        string typeKey,
        Guid entityId,
        string action,
        AclAuditChangeType changeType,
        DateTimeOffset utcNow,
        UserAccountId? changedBy,
        AclRuleDecision? preDecision,
        AclRuleDecision? postDecision,
        int? preOrder,
        int? postOrder,
        EntityAclRule rule
    )
    {
        _ = dbContext.AuthAuditLogs.Add(
            AuthAuditLog.Record(
                typeKey,
                entityId,
                action,
                changeType,
                utcNow,
                changedBy,
                preDecision,
                postDecision,
                preOrder,
                postOrder,
                rule.SpecialRole,
                rule.SecurityRoleId,
                rule.UserAccountId
            )
        );
    }

    private async Task<IReadOnlyList<EntityAclRuleResponse>> LoadResponsesAsync(
        List<EntityAclRule> rules,
        CancellationToken cancellationToken
    )
    {
        var roleNames = await dbContext
            .SecurityRoles.AsNoTracking()
            .ToDictionaryAsync(static r => r.Id, static r => r.Name, cancellationToken);
        var userNames = await dbContext
            .UserAccounts.AsNoTracking()
            .ToDictionaryAsync(static u => u.Id, static u => u.DisplayName, cancellationToken);

        return
        [
            .. rules.Select(r => new EntityAclRuleResponse(
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
            )),
        ];
    }

    private static (SpecialRoleType, Guid?, Guid?) GetPrincipalKey(EntityAclRule rule)
    {
        return (rule.SpecialRole, rule.SecurityRoleId?.Value, rule.UserAccountId?.Value);
    }
}
