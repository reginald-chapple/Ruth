using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Associates a <see cref="UserAccount" /> with a <see cref="SecurityRole" />.</summary>
public sealed class UserRole : IAuditableEntity
{
    private UserRole() { }

    internal UserRole(UserAccountId userAccountId, SecurityRoleId securityRoleId)
    {
        UserAccountId = userAccountId;
        SecurityRoleId = securityRoleId;
    }

    /// <summary>Gets the user account identifier.</summary>
    public UserAccountId UserAccountId { get; private set; }

    /// <summary>Gets the security role identifier.</summary>
    public SecurityRoleId SecurityRoleId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

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
