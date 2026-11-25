using System.Collections.Frozen;
using System.Globalization;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Identity.Domain;

/// <summary>
/// Pure-domain hierarchical Allow/Deny ACL evaluation engine with parent-authority inheritance,
/// cycle protection, and zero external dependencies.
/// </summary>
public static class AclRuleEvaluator
{
    /// <summary>Maximum hierarchy depth walked before aborting to prevent infinite loops.</summary>
    public const int MaxHierarchyDepth = 100;

    /// <summary>Evaluates whether <paramref name="subject" /> is authorized for <paramref name="action" /> on <paramref name="entity" />.</summary>
    public static bool IsAuthorized(
        AclRuleSnapshot snapshot,
        ISecured entity,
        string action,
        SecuritySubject subject
    )
    {
        return Evaluate(snapshot, entity, action, subject).IsAllowed;
    }

    /// <summary>Evaluates authorization and returns a full explanation trace for verification diagnostics.</summary>
    public static AclEvaluationTrace Evaluate(
        AclRuleSnapshot snapshot,
        ISecured entity,
        string action,
        SecuritySubject subject
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(subject);

        var visited = new HashSet<(string TypeKey, Guid EntityId)>();
        var depth = 0;
        var matched = EvaluateNode(
            snapshot,
            entity,
            action.Trim(),
            subject,
            isRootEntity: true,
            checkParentAuthority: true,
            visited,
            ref depth
        );

        if (matched is not null)
        {
            return matched;
        }

        var defaultAllowed = entity.IsAllowedByDefault(action.Trim());
        var defaultText = defaultAllowed ? "Allowed" : "Denied";
        return new AclEvaluationTrace(
            defaultAllowed,
            "Default",
            entity.EntityTypeKey,
            entity.EntityId,
            MatchedRuleId: null,
            MatchedOrder: null,
            $"No matching rule found in entity hierarchy; defaulted to {defaultText} for '{action.Trim()}'."
        );
    }

    /// <summary>
    /// Determines whether an entity action is private to <paramref name="userAccountId" />
    /// (exactly two local rules: Order 0 Allow User, Order 1 Deny AllUsers).
    /// </summary>
    public static bool IsPrivate(
        AclRuleSnapshot snapshot,
        ISecured entity,
        string action,
        Guid userAccountId
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        return userAccountId != Guid.Empty
            && snapshot.TryGetRules(entity.EntityTypeKey, entity.EntityId, action, out var rules)
            && rules.Length == 2
            && IsUserAllowRule(rules[0], userAccountId)
            && IsAllUsersDenyRule(rules[1]);
    }

    private static bool IsUserAllowRule(AclRuleEntry rule, Guid userAccountId)
    {
        return rule.Decision == AclRuleDecision.Allow
            && rule.SpecialRole == SpecialRoleType.None
            && rule.UserAccountId == userAccountId;
    }

    private static bool IsAllUsersDenyRule(AclRuleEntry rule)
    {
        return rule.Decision == AclRuleDecision.Deny
            && rule.SpecialRole == SpecialRoleType.AllUsers;
    }

    private static AclEvaluationTrace? EvaluateNode(
        AclRuleSnapshot snapshot,
        ISecured entity,
        string action,
        SecuritySubject subject,
        bool isRootEntity,
        bool checkParentAuthority,
        HashSet<(string TypeKey, Guid EntityId)> visited,
        ref int depth
    )
    {
        var nodeKey = (entity.EntityTypeKey.ToUpperInvariant(), entity.EntityId);
        if (depth > MaxHierarchyDepth || !visited.Add(nodeKey))
        {
            return null;
        }

        var localMatch = EvaluateLocalRules(snapshot, entity, action, subject, isRootEntity);
        if (localMatch is not null)
        {
            return localMatch;
        }

        if (!checkParentAuthority)
        {
            return null;
        }

        depth++;
        return EvaluateParentChain(
            snapshot,
            entity,
            action,
            subject,
            isRootEntity,
            visited,
            ref depth
        );
    }

    private static AclEvaluationTrace? EvaluateLocalRules(
        AclRuleSnapshot snapshot,
        ISecured entity,
        string action,
        SecuritySubject subject,
        bool isRootEntity
    )
    {
        if (!snapshot.TryGetRules(entity.EntityTypeKey, entity.EntityId, action, out var rules))
        {
            return null;
        }

        foreach (var rule in rules)
        {
            if (RuleMatchesSubject(rule, subject, snapshot.ActiveRoleIds))
            {
                var isAllowed = rule.Decision == AclRuleDecision.Allow;
                var source = isRootEntity ? "LocalRule" : "InheritedRule";
                var explanation = string.Create(
                    CultureInfo.InvariantCulture,
                    $"Matched {source} #{rule.Order} ({rule.Decision}) on {entity.EntityTypeKey} ({entity.EntityId})."
                );
                return new AclEvaluationTrace(
                    isAllowed,
                    source,
                    entity.EntityTypeKey,
                    entity.EntityId,
                    rule.RuleId,
                    rule.Order,
                    explanation
                );
            }
        }

        return null;
    }

    private static AclEvaluationTrace? EvaluateParentChain(
        AclRuleSnapshot snapshot,
        ISecured entity,
        string action,
        SecuritySubject subject,
        bool isRootEntity,
        HashSet<(string TypeKey, Guid EntityId)> visited,
        ref int depth
    )
    {
        if (isRootEntity && entity.ParentAuthorityPre is not null)
        {
            var preResult = EvaluateNode(
                snapshot,
                entity.ParentAuthorityPre,
                action,
                subject,
                isRootEntity: false,
                checkParentAuthority: false,
                visited,
                ref depth
            );
            if (preResult is not null)
            {
                return preResult with { Source = "ParentAuthorityPre" };
            }
        }

        return entity.ParentAuthority is not null
            ? EvaluateNode(
                snapshot,
                entity.ParentAuthority,
                action,
                subject,
                isRootEntity: false,
                checkParentAuthority: true,
                visited,
                ref depth
            )
            : null;
    }

    private static bool RuleMatchesSubject(
        AclRuleEntry rule,
        SecuritySubject subject,
        FrozenSet<Guid> activeRoleIds
    )
    {
        return rule.SpecialRole != SpecialRoleType.None
            ? MatchesSpecialRole(rule.SpecialRole, subject)
            : subject.IsAuthenticated
                && (
                    rule.UserAccountId.HasValue
                        ? rule.UserAccountId.Value == subject.UserAccountId
                        : rule.SecurityRoleId.HasValue
                            && activeRoleIds.Contains(rule.SecurityRoleId.Value)
                            && subject.RoleIds.Contains(rule.SecurityRoleId.Value)
                );
    }

    private static bool MatchesSpecialRole(SpecialRoleType specialRole, SecuritySubject subject)
    {
        return specialRole switch
        {
            SpecialRoleType.AllUsers => true,
            SpecialRoleType.AllAuthenticatedUsers => subject.IsAuthenticated,
            SpecialRoleType.AllUnauthenticatedUsers => !subject.IsAuthenticated,
            SpecialRoleType.None or _ => false,
        };
    }
}
