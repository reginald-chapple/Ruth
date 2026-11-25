namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Represents an immutable ACL change audit log entry.</summary>
public sealed record AuthAuditLogResponse(
    Guid Id,
    string EntityTypeKey,
    Guid EntityId,
    string Action,
    AclAuditChangeType ChangeType,
    DateTimeOffset ChangedOnUtc,
    Guid? ChangedByUserId,
    AclRuleDecision? PreDecision,
    AclRuleDecision? PostDecision,
    int? PreOrder,
    int? PostOrder,
    SpecialRoleType SpecialRole,
    Guid? SecurityRoleId,
    Guid? UserAccountId
);
