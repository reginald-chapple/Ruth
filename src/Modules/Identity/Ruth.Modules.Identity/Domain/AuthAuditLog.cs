using Ruth.Modules.Identity.Contracts.Security;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Immutable audit record emitted whenever an entity ACL rule is added, modified, reordered, or deleted.</summary>
public sealed class AuthAuditLog : Entity<AuthAuditLogId>
{
    private AuthAuditLog() { }

    private AuthAuditLog(
        AuthAuditLogId id,
        string entityTypeKey,
        Guid entityId,
        string action,
        AclAuditChangeType changeType,
        DateTimeOffset changedOnUtc,
        UserAccountId? changedByUserId,
        AclRuleDecision? preDecision,
        AclRuleDecision? postDecision,
        int? preOrder,
        int? postOrder,
        SpecialRoleType specialRole,
        SecurityRoleId? securityRoleId,
        UserAccountId? userAccountId
    )
        : base(id)
    {
        EntityTypeKey = entityTypeKey;
        EntityId = entityId;
        Action = action;
        ChangeType = changeType;
        ChangedOnUtc = changedOnUtc.ToUniversalTime();
        ChangedByUserId = changedByUserId;
        PreDecision = preDecision;
        PostDecision = postDecision;
        PreOrder = preOrder;
        PostOrder = postOrder;
        SpecialRole = specialRole;
        SecurityRoleId = securityRoleId;
        UserAccountId = userAccountId;
    }

    /// <summary>Gets the secured entity type key.</summary>
    public string EntityTypeKey { get; private set; } = string.Empty;

    /// <summary>Gets the secured entity identifier.</summary>
    public Guid EntityId { get; private set; }

    /// <summary>Gets the action name.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Gets the type of ACL mutation.</summary>
    public AclAuditChangeType ChangeType { get; private set; }

    /// <summary>Gets the UTC timestamp of the mutation.</summary>
    public DateTimeOffset ChangedOnUtc { get; private set; }

    /// <summary>Gets the actor user account identifier, if known.</summary>
    public UserAccountId? ChangedByUserId { get; private set; }

    /// <summary>Gets the previous Allow/Deny decision when modifying or deleting.</summary>
    public AclRuleDecision? PreDecision { get; private set; }

    /// <summary>Gets the new Allow/Deny decision when adding or modifying.</summary>
    public AclRuleDecision? PostDecision { get; private set; }

    /// <summary>Gets the previous rule order.</summary>
    public int? PreOrder { get; private set; }

    /// <summary>Gets the new rule order.</summary>
    public int? PostOrder { get; private set; }

    /// <summary>Gets the rule's special role target.</summary>
    public SpecialRoleType SpecialRole { get; private set; }

    /// <summary>Gets the rule's security role target.</summary>
    public SecurityRoleId? SecurityRoleId { get; private set; }

    /// <summary>Gets the rule's user account target.</summary>
    public UserAccountId? UserAccountId { get; private set; }

    /// <summary>Creates an audit log entry.</summary>
    public static AuthAuditLog Record(
        string entityTypeKey,
        Guid entityId,
        string action,
        AclAuditChangeType changeType,
        DateTimeOffset changedOnUtc,
        UserAccountId? changedByUserId,
        AclRuleDecision? preDecision,
        AclRuleDecision? postDecision,
        int? preOrder,
        int? postOrder,
        SpecialRoleType specialRole,
        SecurityRoleId? securityRoleId,
        UserAccountId? userAccountId
    )
    {
        return new AuthAuditLog(
            AuthAuditLogId.Create(),
            entityTypeKey,
            entityId,
            action,
            changeType,
            changedOnUtc,
            changedByUserId,
            preDecision,
            postDecision,
            preOrder,
            postOrder,
            specialRole,
            securityRoleId,
            userAccountId
        );
    }
}
