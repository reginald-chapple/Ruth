using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Associates a <see cref="UserAccount" /> with a <see cref="SecurityRole" />.</summary>
public sealed class UserRole : IAuditableEntity, ITenantScoped
{
    private UserRole() { }

    internal UserRole(UserAccountId userAccountId, SecurityRoleId securityRoleId)
    {
        UserAccountId = userAccountId;
        SecurityRoleId = securityRoleId;
    }

    /// <inheritdoc />
    public TenantId TenantId { get; private set; }

    /// <summary>Gets the user account identifier.</summary>
    public UserAccountId UserAccountId { get; private set; }

    /// <summary>Gets the security role identifier.</summary>
    public SecurityRoleId SecurityRoleId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign UserRole ({UserAccountId}/{SecurityRoleId}) from tenant '{TenantId}' to '{tenantId}'."
            );
        }

        TenantId = tenantId;
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
}
