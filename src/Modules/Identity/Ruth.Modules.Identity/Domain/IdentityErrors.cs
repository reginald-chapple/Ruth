using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Domain;

internal static class IdentityErrors
{
    public static readonly DomainError OnboardingAlreadyCompleted = DomainError.Conflict(
        "Identity.OnboardingAlreadyCompleted",
        "Initial onboarding has already been completed and self-registration is permanently locked."
    );

    public static readonly DomainError OrganizationNameRequired = DomainError.Validation(
        "Identity.OrganizationNameRequired",
        "Organization name is required."
    );

    public static readonly DomainError OrganizationNameTooLong = DomainError.Validation(
        "Identity.OrganizationNameTooLong",
        $"Organization name cannot exceed {OnboardingPolicy.OrganizationNameMaxLength} characters."
    );

    public static readonly DomainError TimeZoneInvalid = DomainError.Validation(
        "Identity.TimeZoneInvalid",
        "A valid system or IANA time zone identifier is required."
    );

    public static readonly DomainError PrimaryBranchNameRequired = DomainError.Validation(
        "Identity.PrimaryBranchNameRequired",
        "Primary branch name is required."
    );

    public static readonly DomainError PrimaryBranchNameTooLong = DomainError.Validation(
        "Identity.PrimaryBranchNameTooLong",
        $"Primary branch name cannot exceed {OnboardingPolicy.BranchNameMaxLength} characters."
    );

    public static readonly DomainError PrimaryBranchShortCodeTooLong = DomainError.Validation(
        "Identity.PrimaryBranchShortCodeTooLong",
        $"Primary branch short code cannot exceed {OnboardingPolicy.BranchShortCodeMaxLength} characters."
    );

    public static readonly DomainError UserNameRequired = DomainError.Validation(
        "Identity.UserNameRequired",
        "Username is required."
    );

    public static readonly DomainError UserNameTooLong = DomainError.Validation(
        "Identity.UserNameTooLong",
        $"Username cannot exceed {OnboardingPolicy.UserNameMaxLength} characters."
    );

    public static readonly DomainError EmailInvalid = DomainError.Validation(
        "Identity.EmailInvalid",
        "A valid email address is required."
    );

    public static readonly DomainError EmailTooLong = DomainError.Validation(
        "Identity.EmailTooLong",
        $"Email address cannot exceed {OnboardingPolicy.EmailMaxLength} characters."
    );

    public static readonly DomainError DisplayNameRequired = DomainError.Validation(
        "Identity.DisplayNameRequired",
        "Display name is required."
    );

    public static readonly DomainError DisplayNameTooLong = DomainError.Validation(
        "Identity.DisplayNameTooLong",
        $"Display name cannot exceed {OnboardingPolicy.DisplayNameMaxLength} characters."
    );

    public static readonly DomainError PasswordWeak = DomainError.Validation(
        "Identity.PasswordWeak",
        $"Password must be at least {OnboardingPolicy.PasswordMinLength} characters and contain uppercase, lowercase, digit, and symbol characters."
    );

    public static readonly DomainError PasswordTooLong = DomainError.Validation(
        "Identity.PasswordTooLong",
        $"Password cannot exceed {OnboardingPolicy.PasswordMaxLength} characters."
    );

    public static readonly DomainError PasswordHashRequired = DomainError.Validation(
        "Identity.PasswordHashRequired",
        "Password hash is required."
    );

    public static readonly DomainError RoleNameRequired = DomainError.Validation(
        "Identity.RoleNameRequired",
        "Security role name is required."
    );

    public static readonly DomainError RoleNameTooLong = DomainError.Validation(
        "Identity.RoleNameTooLong",
        $"Security role name cannot exceed {OnboardingPolicy.RoleNameMaxLength} characters."
    );

    public static readonly DomainError RoleDescriptionTooLong = DomainError.Validation(
        "Identity.RoleDescriptionTooLong",
        "Security role description cannot exceed 500 characters."
    );

    public static readonly DomainError RoleAlreadyAssigned = DomainError.Conflict(
        "Identity.RoleAlreadyAssigned",
        "The security role is already assigned to this user account."
    );

    public static readonly DomainError RoleNotAssigned = DomainError.NotFound(
        "Identity.RoleNotAssigned",
        "The security role is not assigned to this user account."
    );

    public static readonly DomainError RoleNotFound = DomainError.NotFound(
        "Identity.RoleNotFound",
        "Security role was not found."
    );

    public static readonly DomainError RoleAlreadyExists = DomainError.Conflict(
        "Identity.RoleAlreadyExists",
        "A security role with that name already exists."
    );

    public static readonly DomainError SystemRoleProtected = DomainError.Forbidden(
        "Identity.SystemRoleProtected",
        "Built-in system roles cannot be renamed, deleted, or deactivated."
    );

    public static readonly DomainError InvalidControlCharacters = DomainError.Validation(
        "Identity.InvalidControlCharacters",
        "Input text cannot contain null bytes or control characters."
    );

    public static readonly DomainError InvalidCredentials = DomainError.Unauthorized(
        "Identity.InvalidCredentials",
        "Invalid username or password."
    );

    public static readonly DomainError AccountLockedOut = DomainError.Forbidden(
        "Identity.AccountLockedOut",
        "This user account is temporarily locked out due to repeated failed login attempts."
    );

    public static readonly DomainError AccountNotConfirmed = DomainError.Forbidden(
        "Identity.AccountNotConfirmed",
        "This user account has not been confirmed yet."
    );

    public static readonly DomainError AccountInactive = DomainError.Forbidden(
        "Identity.AccountInactive",
        "This user account is inactive."
    );

    public static readonly DomainError UserAccountNotFound = DomainError.NotFound(
        "Identity.UserAccountNotFound",
        "User account was not found."
    );

    public static readonly DomainError UserAlreadyExists = DomainError.Conflict(
        "Identity.UserAlreadyExists",
        "A user account with that username or email already exists."
    );

    public static readonly DomainError RefreshTokenInvalid = DomainError.Unauthorized(
        "Identity.RefreshTokenInvalid",
        "The refresh token is invalid, expired, or has been revoked."
    );

    public static readonly DomainError AccountTokenInvalid = DomainError.Validation(
        "Identity.AccountTokenInvalid",
        "The account token is invalid, expired, or has already been used."
    );

    public static readonly DomainError ExternalProviderNotFound = DomainError.NotFound(
        "Identity.ExternalProviderNotFound",
        "External identity provider was not found or is disabled."
    );

    public static readonly DomainError ExternalProviderInvalid = DomainError.Validation(
        "Identity.ExternalProviderInvalid",
        "External identity provider configuration is invalid."
    );

    public static readonly DomainError RemoteAuthSessionInvalid = DomainError.Unauthorized(
        "Identity.RemoteAuthSessionInvalid",
        "The remote authentication session is invalid, expired, or already completed."
    );

    public static readonly DomainError ExternalLoginUnlinked = DomainError.Forbidden(
        "Identity.ExternalLoginUnlinked",
        "No user account is linked to this external identity."
    );

    public static readonly DomainError AclRuleTargetInvalid = DomainError.Validation(
        "Identity.AclRuleTargetInvalid",
        "An ACL rule must target exactly one of SpecialRole, SecurityRoleId, or UserAccountId."
    );

    public static readonly DomainError AclEntityTypeRequired = DomainError.Validation(
        "Identity.AclEntityTypeRequired",
        "Entity type key is required for an ACL rule."
    );

    public static readonly DomainError AclActionRequired = DomainError.Validation(
        "Identity.AclActionRequired",
        "Authorization action is required for an ACL rule."
    );

    public static readonly DomainError ApiKeyNameRequired = DomainError.Validation(
        "Identity.ApiKeyNameRequired",
        "API key name is required."
    );

    public static readonly DomainError ApiKeyNotFound = DomainError.NotFound(
        "Identity.ApiKeyNotFound",
        "REST API key was not found."
    );

    public static readonly DomainError AuthClientInvalid = DomainError.Validation(
        "Identity.AuthClientInvalid",
        "OAuth/OIDC client configuration is invalid."
    );

    public static readonly DomainError AuthClientAlreadyExists = DomainError.Conflict(
        "Identity.AuthClientAlreadyExists",
        "An OAuth/OIDC client with that client ID already exists."
    );

    public static readonly DomainError AuthClientNotFound = DomainError.NotFound(
        "Identity.AuthClientNotFound",
        "OAuth/OIDC client was not found."
    );

    public static readonly DomainError AuthScopeInvalid = DomainError.Validation(
        "Identity.AuthScopeInvalid",
        "OAuth/OIDC scope name is required and cannot exceed 50 characters."
    );

    public static readonly DomainError AuthScopeAlreadyExists = DomainError.Conflict(
        "Identity.AuthScopeAlreadyExists",
        "An OAuth/OIDC scope with that name already exists."
    );
}
