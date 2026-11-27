using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Security role entity used to group users for hierarchical ACL authorization.</summary>
public sealed class SecurityRole
    : AggregateRoot<SecurityRoleId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    /// <summary>Deterministic well-known identifier for the system Administrators role.</summary>
    public static readonly SecurityRoleId AdministratorsRoleId = SecurityRoleId.From(
        new Guid(0x628c51a8, 0x4613, 0x43ed, 0xa1, 0x8d, 0x4a, 0x6f, 0xb9, 0x99, 0x27, 0x3e)
    );

    /// <summary>Display name of the system Administrators role.</summary>
    public const string AdministratorsRoleName = "Administrators";

    /// <summary>Normalized uppercase name of the system Administrators role.</summary>
    public const string AdministratorsNormalizedName = "ADMINISTRATORS";

    /// <summary>Maximum description length for a security role.</summary>
    public const int DescriptionMaxLength = 500;

    private SecurityRole() { }

    private SecurityRole(
        SecurityRoleId id,
        string name,
        string? description,
        bool isSystem,
        bool isActive
    )
        : base(id)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        IsSystem = isSystem;
        IsActive = isActive;
    }

    /// <summary>Gets the role display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the normalized uppercase role name.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the optional role description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets a value indicating whether this role is a built-in system role.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets a value indicating whether this role is active for ACL evaluation.</summary>
    public bool IsActive { get; private set; } = true;

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>Derives the collision-free per-tenant Administrators role identifier.</summary>
    public static SecurityRoleId GetAdministratorsRoleId(TenantId tenantId)
    {
        return SecurityRoleId.From(tenantId.DeriveDeterministicGuid(AdministratorsRoleId.Value));
    }

    /// <summary>Creates the built-in system Administrators role.</summary>
    public static SecurityRole CreateSystemAdministrators(TenantId tenantId = default)
    {
        return new SecurityRole(
            GetAdministratorsRoleId(tenantId),
            AdministratorsRoleName,
            "Built-in system administrators with full administrative authority.",
            isSystem: true,
            isActive: true
        );
    }

    /// <summary>Creates a validated security role.</summary>
    public static Result<SecurityRole> Create(
        SecurityRoleId id,
        string name,
        bool isSystem = false,
        string? description = null
    )
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
        var descriptionResult = OnboardingPolicy.ValidateOptionalText(
            description,
            DescriptionMaxLength,
            IdentityErrors.RoleDescriptionTooLong
        );
        var combined = Result.Combine(nameResult, descriptionResult);

        return combined.IsFailure
            ? Result.Failure<SecurityRole>(combined.Errors)
            : new SecurityRole(
                id,
                nameResult.Value,
                descriptionResult.Value,
                isSystem,
                isActive: true
            );
    }

    /// <summary>Updates a security role's name, description, and active flag.</summary>
    public Result Update(string name, string? description, bool isActive)
    {
        if (IsSystem && !isActive)
        {
            return IdentityErrors.SystemRoleProtected;
        }

        var nameResult = OnboardingPolicy.ValidateRequiredText(
            name,
            OnboardingPolicy.RoleNameMaxLength,
            IdentityErrors.RoleNameRequired,
            IdentityErrors.RoleNameTooLong
        );
        var descriptionResult = OnboardingPolicy.ValidateOptionalText(
            description,
            DescriptionMaxLength,
            IdentityErrors.RoleDescriptionTooLong
        );
        var combined = Result.Combine(nameResult, descriptionResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        if (IsSystem && !string.Equals(nameResult.Value, Name, StringComparison.Ordinal))
        {
            return IdentityErrors.SystemRoleProtected;
        }

        Name = nameResult.Value;
        NormalizedName = nameResult.Value.ToUpperInvariant();
        Description = descriptionResult.Value;
        IsActive = isActive;
        return Result.Success();
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
