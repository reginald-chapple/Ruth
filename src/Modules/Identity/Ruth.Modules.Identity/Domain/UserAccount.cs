using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Minimal administrator user account aggregate for Wave 0 initial setup.</summary>
public sealed class UserAccount
    : AggregateRoot<UserAccountId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private readonly List<UserRole> _roles = [];

    private UserAccount() { }

    private UserAccount(
        UserAccountId id,
        string userName,
        string email,
        string displayName,
        string passwordHash,
        DateTimeOffset createdOnUtc
    )
        : base(id)
    {
        UserName = userName;
        NormalizedUserName = userName.ToUpperInvariant();
        Email = email;
        NormalizedEmail = email.ToUpperInvariant();
        DisplayName = displayName;
        PasswordHash = passwordHash;
        IsConfirmed = true;
        IsLockedOut = false;
        LastPasswordChangedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <summary>Gets the account login username.</summary>
    public string UserName { get; private set; } = string.Empty;

    /// <summary>Gets the normalized uppercase username.</summary>
    public string NormalizedUserName { get; private set; } = string.Empty;

    /// <summary>Gets the account email address.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets the normalized uppercase email address.</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    /// <summary>Gets the account display name.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Gets the salted cryptographic password hash.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the account is confirmed.</summary>
    public bool IsConfirmed { get; private set; }

    /// <summary>Gets a value indicating whether the account is locked out.</summary>
    public bool IsLockedOut { get; private set; }

    /// <summary>Gets the UTC timestamp when the password was last changed.</summary>
    public DateTimeOffset? LastPasswordChangedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>Gets the security roles assigned to this account.</summary>
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    /// <summary>Creates a confirmed initial administrator account assigned to <see cref="SecurityRole.AdministratorsRoleId" />.</summary>
    public static Result<UserAccount> CreateAdministrator(
        string userName,
        string email,
        string displayName,
        string passwordHash,
        DateTimeOffset createdOnUtc
    )
    {
        var userNameResult = OnboardingPolicy.ValidateRequiredText(
            userName,
            OnboardingPolicy.UserNameMaxLength,
            IdentityErrors.UserNameRequired,
            IdentityErrors.UserNameTooLong
        );
        var emailResult = OnboardingPolicy.ValidateEmail(email);
        var displayNameResult = OnboardingPolicy.ValidateRequiredText(
            displayName,
            OnboardingPolicy.DisplayNameMaxLength,
            IdentityErrors.DisplayNameRequired,
            IdentityErrors.DisplayNameTooLong
        );
        var hashValidation = string.IsNullOrWhiteSpace(passwordHash)
            ? Result.Failure(IdentityErrors.PasswordHashRequired)
            : Result.Success();

        var combined = Result.Combine(
            userNameResult,
            emailResult,
            displayNameResult,
            hashValidation
        );
        if (combined.IsFailure)
        {
            return Result.Failure<UserAccount>(combined.Errors);
        }

        var account = new UserAccount(
            UserAccountId.Create(),
            userNameResult.Value,
            emailResult.Value,
            displayNameResult.Value,
            passwordHash.Trim(),
            createdOnUtc
        );
        _ = account.AddRole(SecurityRole.AdministratorsRoleId);
        return account;
    }

    /// <summary>Assigns a security role to this account unless already assigned.</summary>
    public Result AddRole(SecurityRoleId roleId)
    {
        if (roleId == SecurityRoleId.Empty)
        {
            return IdentityErrors.RoleNameRequired;
        }

        if (_roles.Exists(role => role.SecurityRoleId == roleId))
        {
            return IdentityErrors.RoleAlreadyAssigned;
        }

        _roles.Add(new UserRole(Id, roleId));
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

    /// <inheritdoc />
    public override string ToString()
    {
        return $"UserAccount({Id.Value}, {UserName})";
    }
}
