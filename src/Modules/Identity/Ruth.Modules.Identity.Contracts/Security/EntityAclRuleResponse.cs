namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Represents a persisted entity ACL rule.</summary>
public sealed record EntityAclRuleResponse(
    Guid Id,
    string EntityTypeKey,
    Guid EntityId,
    string Action,
    int Order,
    AclRuleDecision Decision,
    SpecialRoleType SpecialRole,
    Guid? SecurityRoleId,
    string? SecurityRoleName,
    Guid? UserAccountId,
    string? UserDisplayName
);
