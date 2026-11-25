using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Lock-free immutable ACL rule snapshot backed by <see cref="FrozenDictionary{TKey,TValue}" />.</summary>
public sealed class AclRuleSnapshot
{
    /// <summary>Gets an empty snapshot with no rules or active roles.</summary>
    public static AclRuleSnapshot Empty { get; } =
        new(FrozenDictionary<AclLookupKey, ImmutableArray<AclRuleEntry>>.Empty, []);

    private AclRuleSnapshot(
        FrozenDictionary<AclLookupKey, ImmutableArray<AclRuleEntry>> rulesByKey,
        FrozenSet<Guid> activeRoleIds
    )
    {
        RulesByKey = rulesByKey;
        ActiveRoleIds = activeRoleIds;
    }

    /// <summary>Gets the frozen map of ordered ACL rules keyed by (EntityTypeKey, EntityId, Action).</summary>
    public FrozenDictionary<AclLookupKey, ImmutableArray<AclRuleEntry>> RulesByKey { get; }

    /// <summary>Gets the frozen set of currently active security role IDs.</summary>
    public FrozenSet<Guid> ActiveRoleIds { get; }

    /// <summary>Builds a frozen snapshot from persisted rules and active role IDs.</summary>
    public static AclRuleSnapshot Create(
        IEnumerable<EntityAclRule> rules,
        IEnumerable<Guid> activeRoleIds
    )
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(activeRoleIds);

        var grouped = rules
            .GroupBy(static r => new AclLookupKey(r.EntityTypeKey, r.EntityId, r.Action))
            .ToFrozenDictionary(
                static g => g.Key,
                static g =>
                    g.OrderBy(static r => r.Order)
                        .ThenBy(static r => r.Id.Value)
                        .Select(static r => new AclRuleEntry(
                            r.Id.Value,
                            r.Order,
                            r.Decision,
                            r.SpecialRole,
                            r.SecurityRoleId?.Value,
                            r.UserAccountId?.Value
                        ))
                        .ToImmutableArray()
            );

        return new AclRuleSnapshot(grouped, activeRoleIds.ToFrozenSet());
    }

    /// <summary>Attempts to retrieve the ordered rules for the specified entity and action.</summary>
    public bool TryGetRules(
        string entityTypeKey,
        Guid entityId,
        string action,
        out ImmutableArray<AclRuleEntry> rules
    )
    {
        return RulesByKey.TryGetValue(new AclLookupKey(entityTypeKey, entityId, action), out rules);
    }
}
