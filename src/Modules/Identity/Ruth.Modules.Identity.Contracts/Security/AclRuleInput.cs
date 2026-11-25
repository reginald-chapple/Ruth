namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Ordered input rule when replacing ACL rules for an entity action.</summary>
public sealed record AclRuleInput(
    AclRuleDecision Decision,
    SpecialRoleType SpecialRole,
    Guid? SecurityRoleId = null,
    Guid? UserAccountId = null
);
