using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Domain;

/// <summary>User account aggregate governing credentials, lockout state, and security role membership.</summary>
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
        bool isConfirmed,
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
        IsConfirmed = isConfirmed;
        IsActive = true;
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

    /// <summary>Gets a value indicating whether the account is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets a value indicating whether the account is locked out.</summary>
    public bool IsLockedOut { get; private set; }

    /// <summary>Gets a value indicating whether the user must change their password on next login.</summary>
    public bool IsPasswordChangeRequired { get; private set; }

    /// <summary>Gets the UTC timestamp until which automatic lockout remains in effect.</summary>
    public DateTimeOffset? LockedOutUntilOnUtc { get; private set; }

    /// <summary>Gets the number of consecutive failed password attempts in the current window.</summary>
    public int FailedPasswordAttemptCount { get; private set; }

    /// <summary>Gets the UTC start timestamp of the failed password attempt window.</summary>
    public DateTimeOffset? FailedPasswordAttemptWindowStartOnUtc { get; private set; }

    /// <summary>Gets the UTC timestamp of the most recent successful login.</summary>
    public DateTimeOffset? LastLoginOnUtc { get; private set; }

    /// <summary>Gets the UTC timestamp of the most recent account activity.</summary>
    public DateTimeOffset? LastActivityOnUtc { get; private set; }

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
        var result = Create(
            userName,
            email,
            displayName,
            passwordHash,
            isConfirmed: true,
            createdOnUtc
        );
        if (result.IsFailure)
        {
            return result;
        }

        _ = result.Value.AddRole(SecurityRole.AdministratorsRoleId);
        return result;
    }

    /// <summary>Creates a validated user account.</summary>
    public static Result<UserAccount> Create(
        string userName,
        string email,
        string displayName,
        string passwordHash,
        bool isConfirmed,
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
        return combined.IsFailure
            ? Result.Failure<UserAccount>(combined.Errors)
            : new UserAccount(
                UserAccountId.Create(),
                userNameResult.Value,
                emailResult.Value,
                displayNameResult.Value,
                passwordHash.Trim(),
                isConfirmed,
                createdOnUtc
            );
    }

    /// <summary>Evaluates whether the account is currently locked out at <paramref name="utcNow" />.</summary>
    public bool IsCurrentlyLockedOut(DateTimeOffset utcNow)
    {
        return IsLockedOut
            && (
                !LockedOutUntilOnUtc.HasValue
                || LockedOutUntilOnUtc.Value > utcNow.ToUniversalTime()
            );
    }

    /// <summary>Records a successful authentication, clearing failed attempts and expired lockouts.</summary>
    public void RecordSuccessfulLogin(DateTimeOffset utcNow)
    {
        var now = utcNow.ToUniversalTime();
        LastLoginOnUtc = now;
        LastActivityOnUtc = now;
        FailedPasswordAttemptCount = 0;
        FailedPasswordAttemptWindowStartOnUtc = null;
        IsLockedOut = false;
        LockedOutUntilOnUtc = null;
    }

    /// <summary>Records a failed password login attempt and applies lockout when the threshold is reached.</summary>
    public void RecordFailedLogin(
        DateTimeOffset utcNow,
        int maxAttempts = AuthenticationPolicy.MaxFailedPasswordAttempts,
        TimeSpan? attemptWindow = null,
        TimeSpan? lockoutDuration = null
    )
    {
        var now = utcNow.ToUniversalTime();
        var window = attemptWindow ?? AuthenticationPolicy.FailedAttemptWindow;
        var duration = lockoutDuration ?? AuthenticationPolicy.LockoutDuration;

        if (
            !FailedPasswordAttemptWindowStartOnUtc.HasValue
            || (now - FailedPasswordAttemptWindowStartOnUtc.Value) > window
        )
        {
            FailedPasswordAttemptWindowStartOnUtc = now;
            FailedPasswordAttemptCount = 1;
        }
        else
        {
            FailedPasswordAttemptCount++;
        }

        if (FailedPasswordAttemptCount >= maxAttempts)
        {
            IsLockedOut = true;
            LockedOutUntilOnUtc = now.Add(duration);
        }
    }

    /// <summary>Updates the account password hash and clears lockout / required password change flags.</summary>
    public Result ChangePassword(string newPasswordHash, DateTimeOffset changedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            return IdentityErrors.PasswordHashRequired;
        }

        var now = changedOnUtc.ToUniversalTime();
        PasswordHash = newPasswordHash.Trim();
        LastPasswordChangedOnUtc = now;
        LastActivityOnUtc = now;
        IsPasswordChangeRequired = false;
        Unlock();
        return Result.Success();
    }

    /// <summary>Marks the user account as confirmed.</summary>
    public void ConfirmAccount()
    {
        IsConfirmed = true;
    }

    /// <summary>Unlocks the account and resets failed password attempt counters.</summary>
    public void Unlock()
    {
        IsLockedOut = false;
        LockedOutUntilOnUtc = null;
        FailedPasswordAttemptCount = 0;
        FailedPasswordAttemptWindowStartOnUtc = null;
    }

    /// <summary>Updates whether this account is active.</summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>Sets whether a password change is required at next sign-in.</summary>
    public void SetPasswordChangeRequired(bool required)
    {
        IsPasswordChangeRequired = required;
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

    /// <summary>Removes a security role assignment from this account.</summary>
    public Result RemoveRole(SecurityRoleId roleId)
    {
        var index = _roles.FindIndex(role => role.SecurityRoleId == roleId);
        if (index < 0)
        {
            return IdentityErrors.RoleNotAssigned;
        }

        _roles.RemoveAt(index);
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
