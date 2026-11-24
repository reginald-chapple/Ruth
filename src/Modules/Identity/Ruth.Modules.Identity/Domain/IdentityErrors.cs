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

    public static readonly DomainError PrimaryCampusNameRequired = DomainError.Validation(
        "Identity.PrimaryCampusNameRequired",
        "Primary campus name is required."
    );

    public static readonly DomainError PrimaryCampusNameTooLong = DomainError.Validation(
        "Identity.PrimaryCampusNameTooLong",
        $"Primary campus name cannot exceed {OnboardingPolicy.CampusNameMaxLength} characters."
    );

    public static readonly DomainError PrimaryCampusShortCodeTooLong = DomainError.Validation(
        "Identity.PrimaryCampusShortCodeTooLong",
        $"Primary campus short code cannot exceed {OnboardingPolicy.CampusShortCodeMaxLength} characters."
    );

    public static readonly DomainError UserNameRequired = DomainError.Validation(
        "Identity.UserNameRequired",
        "Administrator username is required."
    );

    public static readonly DomainError UserNameTooLong = DomainError.Validation(
        "Identity.UserNameTooLong",
        $"Administrator username cannot exceed {OnboardingPolicy.UserNameMaxLength} characters."
    );

    public static readonly DomainError EmailInvalid = DomainError.Validation(
        "Identity.EmailInvalid",
        "A valid administrator email address is required."
    );

    public static readonly DomainError EmailTooLong = DomainError.Validation(
        "Identity.EmailTooLong",
        $"Administrator email address cannot exceed {OnboardingPolicy.EmailMaxLength} characters."
    );

    public static readonly DomainError DisplayNameRequired = DomainError.Validation(
        "Identity.DisplayNameRequired",
        "Administrator display name is required."
    );

    public static readonly DomainError DisplayNameTooLong = DomainError.Validation(
        "Identity.DisplayNameTooLong",
        $"Administrator display name cannot exceed {OnboardingPolicy.DisplayNameMaxLength} characters."
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

    public static readonly DomainError RoleAlreadyAssigned = DomainError.Conflict(
        "Identity.RoleAlreadyAssigned",
        "The security role is already assigned to this user account."
    );

    public static readonly DomainError InvalidControlCharacters = DomainError.Validation(
        "Identity.InvalidControlCharacters",
        "Input text cannot contain null bytes or control characters."
    );
}
