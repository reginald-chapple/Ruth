using System.Net.Mail;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Domain;

internal static class OnboardingPolicy
{
    public const int OrganizationNameMaxLength = 200;
    public const int TimeZoneIdMaxLength = 50;
    public const int CampusNameMaxLength = 100;
    public const int CampusShortCodeMaxLength = 50;
    public const int UserNameMaxLength = 100;
    public const int EmailMaxLength = 256;
    public const int DisplayNameMaxLength = 150;
    public const int RoleNameMaxLength = 100;
    public const int PasswordMinLength = 12;
    public const int PasswordMaxLength = 128;

    public static Result<string> ValidateRequiredText(
        string? value,
        int maxLength,
        DomainError requiredError,
        DomainError tooLongError
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return requiredError;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? tooLongError
            : ContainsControlCharacters(trimmed) ? IdentityErrors.InvalidControlCharacters
            : trimmed;
    }

    public static Result<string?> ValidateOptionalText(
        string? value,
        int maxLength,
        DomainError tooLongError
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<string?>(value: null);
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? tooLongError
            : ContainsControlCharacters(trimmed) ? IdentityErrors.InvalidControlCharacters
            : Result.Success<string?>(trimmed);
    }

    public static Result<string> ValidateTimeZoneId(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return IdentityErrors.TimeZoneInvalid;
        }

        var trimmed = timeZoneId.Trim();
        return
            trimmed.Length <= TimeZoneIdMaxLength
            && !ContainsControlCharacters(trimmed)
            && TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out var resolved)
            ? resolved.Id
            : IdentityErrors.TimeZoneInvalid;
    }

    public static Result<string> ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return IdentityErrors.EmailInvalid;
        }

        var trimmed = email.Trim();
        return trimmed.Length > EmailMaxLength ? IdentityErrors.EmailTooLong
            : ContainsControlCharacters(trimmed)
            || !MailAddress.TryCreate(trimmed, out var parsed)
            || !string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase)
            || !trimmed.Contains('.', StringComparison.Ordinal)
                ? IdentityErrors.EmailInvalid
            : trimmed;
    }

    public static Result ValidatePasswordComplexity(string? password)
    {
        return string.IsNullOrEmpty(password) || password.Length < PasswordMinLength
                ? IdentityErrors.PasswordWeak
            : password.Length > PasswordMaxLength ? IdentityErrors.PasswordTooLong
            : HasRequiredCharacterClasses(password) ? Result.Success()
            : IdentityErrors.PasswordWeak;
    }

    private static bool HasRequiredCharacterClasses(string password)
    {
        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        var hasNonAlphanumeric = false;

        foreach (var ch in password)
        {
            if (char.IsUpper(ch))
            {
                hasUpper = true;
            }
            else if (char.IsLower(ch))
            {
                hasLower = true;
            }
            else if (char.IsDigit(ch))
            {
                hasDigit = true;
            }
            else if (!char.IsControl(ch))
            {
                hasNonAlphanumeric = true;
            }
        }

        return hasUpper && hasLower && hasDigit && hasNonAlphanumeric;
    }

    private static bool ContainsControlCharacters(string value)
    {
        foreach (var ch in value)
        {
            if (char.IsControl(ch))
            {
                return true;
            }
        }

        return false;
    }
}
