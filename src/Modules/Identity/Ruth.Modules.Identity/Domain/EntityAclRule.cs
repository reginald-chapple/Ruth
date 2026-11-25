using Ruth.Modules.Identity.Contracts.Security;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Ordered Allow/Deny ACL rule attached to a securable entity type/instance and action.</summary>
public sealed class EntityAclRule : Entity<EntityAclRuleId>, IAuditableEntity, IHasConcurrencyToken
{
    private EntityAclRule() { }

    private EntityAclRule(
        EntityAclRuleId id,
        string entityTypeKey,
        Guid entityId,
        string action,
        int order,
        AclRuleDecision decision,
        SpecialRoleType specialRole,
        SecurityRoleId? securityRoleId,
        UserAccountId? userAccountId
    )
        : base(id)
    {
        EntityTypeKey = entityTypeKey;
        EntityId = entityId;
        Action = action;
        Order = order;
        Decision = decision;
        SpecialRole = specialRole;
        SecurityRoleId = securityRoleId;
        UserAccountId = userAccountId;
    }

    /// <summary>Gets the canonical entity type key.</summary>
    public string EntityTypeKey { get; private set; } = string.Empty;

    /// <summary>Gets the entity instance identifier, or <see cref="Guid.Empty" /> for type/global defaults.</summary>
    public Guid EntityId { get; private set; }

    /// <summary>Gets the authorization action name (e.g., View, Edit, Administrate).</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Gets the zero-based evaluation priority order (lowest evaluated first).</summary>
    public int Order { get; private set; }

    /// <summary>Gets whether matching callers are allowed or denied.</summary>
    public AclRuleDecision Decision { get; private set; }

    /// <summary>Gets the special role target, or <see cref="SpecialRoleType.None" />.</summary>
    public SpecialRoleType SpecialRole { get; private set; }

    /// <summary>Gets the optional security role target.</summary>
    public SecurityRoleId? SecurityRoleId { get; private set; }

    /// <summary>Gets the optional user account target.</summary>
    public UserAccountId? UserAccountId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>Creates a validated entity ACL rule.</summary>
    public static Result<EntityAclRule> Create(
        string entityTypeKey,
        Guid entityId,
        string action,
        int order,
        AclRuleDecision decision,
        SpecialRoleType specialRole,
        SecurityRoleId? securityRoleId = null,
        UserAccountId? userAccountId = null
    )
    {
        if (string.IsNullOrWhiteSpace(entityTypeKey))
        {
            return IdentityErrors.AclEntityTypeRequired;
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            return IdentityErrors.AclActionRequired;
        }

        var normalizedRoleId =
            securityRoleId.HasValue && securityRoleId.Value != Domain.SecurityRoleId.Empty
                ? securityRoleId
                : null;
        var normalizedUserId =
            userAccountId.HasValue && userAccountId.Value != Domain.UserAccountId.Empty
                ? userAccountId
                : null;

        var targetCount =
            (specialRole != SpecialRoleType.None ? 1 : 0)
            + (normalizedRoleId.HasValue ? 1 : 0)
            + (normalizedUserId.HasValue ? 1 : 0);

        return targetCount != 1
            ? (Result<EntityAclRule>)IdentityErrors.AclRuleTargetInvalid
            : (Result<EntityAclRule>)
                new EntityAclRule(
                    EntityAclRuleId.Create(),
                    entityTypeKey.Trim(),
                    entityId,
                    action.Trim(),
                    Math.Max(0, order),
                    decision,
                    specialRole,
                    normalizedRoleId,
                    normalizedUserId
                );
    }

    /// <summary>Updates rule priority order.</summary>
    public void SetOrder(int order)
    {
        Order = Math.Max(0, order);
    }

    /// <summary>Updates Allow/Deny decision.</summary>
    public void SetDecision(AclRuleDecision decision)
    {
        Decision = decision;
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
