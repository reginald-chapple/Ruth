using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Minimal security role entity for initial administrator authorization.</summary>
public sealed class SecurityRole : Entity<SecurityRoleId>, IAuditableEntity, IHasConcurrencyToken
{
    /// <summary>Deterministic well-known identifier for the system Administrators role.</summary>
    public static readonly SecurityRoleId AdministratorsRoleId = SecurityRoleId.From(
        new Guid(0x628c51a8, 0x4613, 0x43ed, 0xa1, 0x8d, 0x4a, 0x6f, 0xb9, 0x99, 0x27, 0x3e)
    );

    /// <summary>Display name of the system Administrators role.</summary>
    public const string AdministratorsRoleName = "Administrators";

    /// <summary>Normalized uppercase name of the system Administrators role.</summary>
    public const string AdministratorsNormalizedName = "ADMINISTRATORS";

    private SecurityRole() { }

    private SecurityRole(SecurityRoleId id, string name, bool isSystem)
        : base(id)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        IsSystem = isSystem;
    }

    /// <summary>Gets the role display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the normalized uppercase role name.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this role is a built-in system role.</summary>
    public bool IsSystem { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>Creates the built-in system Administrators role.</summary>
    public static SecurityRole CreateSystemAdministrators()
    {
        return new SecurityRole(AdministratorsRoleId, AdministratorsRoleName, isSystem: true);
    }

    /// <summary>Creates a validated security role.</summary>
    public static Result<SecurityRole> Create(SecurityRoleId id, string name, bool isSystem = false)
    {
        if (id == SecurityRoleId.Empty)
        {
            return IdentityErrors.RoleNameRequired;
        }

        var nameResult = OnboardingPolicy.ValidateRequiredText(
            name,
            OnboardingPolicy.RoleNameMaxLength,
            IdentityErrors.RoleNameRequired,
            IdentityErrors.RoleNameTooLong
        );

        return nameResult.IsFailure
            ? Result.Failure<SecurityRole>(nameResult.Errors)
            : new SecurityRole(id, nameResult.Value, isSystem);
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
