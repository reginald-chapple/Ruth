using Ruth.Modules.Identity.Contracts.Security;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Lightweight immutable ACL rule projection stored inside an <see cref="AclRuleSnapshot" />.</summary>
public sealed record AclRuleEntry(
    Guid RuleId,
    int Order,
    AclRuleDecision Decision,
    SpecialRoleType SpecialRole,
    Guid? SecurityRoleId,
    Guid? UserAccountId
);
