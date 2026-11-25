using System.Net.Mail;
using System.Text;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain;

internal static class CrmTextPolicy
{
    public const int NamePartMaxLength = 50;
    public const int EmailMaxLength = 75;
    public const int EmailNoteMaxLength = 250;
    public const int SearchValueMaxLength = 255;
    public const int HouseholdNameMaxLength = 100;
    public const int SignalNameMaxLength = 100;
    public const int DescriptionMaxLength = 2000;

    public static string SanitizePersonName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousWasSpace = true;

        foreach (var ch in value)
        {
            if (char.IsControl(ch) || char.IsSurrogate(ch))
            {
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasSpace)
                {
                    _ = builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            previousWasSpace = false;
            _ = builder.Append(
                ch switch
                {
                    '\u2018' or '\u2019' or '\u2032' => '\'',
                    '\u201C' or '\u201D' => '"',
                    _ => ch,
                }
            );
        }

        return builder.ToString().TrimEnd();
    }

    public static Result<string> ValidateRequiredText(
        string? value,
        int maxLength,
        DomainError requiredError,
        DomainError tooLongError,
        bool allowNewLines = false
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return requiredError;
        }

        var trimmed = value.Trim();
        return ContainsInvalidControlChars(trimmed, allowNewLines)
                ? CrmErrors.InvalidControlCharacters
            : trimmed.Length > maxLength ? tooLongError
            : Result.Success(trimmed);
    }

    public static Result<string?> ValidateOptionalText(
        string? value,
        int maxLength,
        DomainError? tooLongError = null,
        bool allowNewLines = true
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<string?>(value: null);
        }

        var trimmed = value.Trim();
        return ContainsInvalidControlChars(trimmed, allowNewLines)
                ? CrmErrors.InvalidControlCharacters
            : trimmed.Length > maxLength ? tooLongError ?? CrmErrors.TextTooLong
            : Result.Success<string?>(trimmed);
    }

    public static Result<string?> ValidateOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Success<string?>(value: null);
        }

        var trimmed = email.Trim();
        return trimmed.Length > EmailMaxLength
            || ContainsInvalidControlChars(trimmed, allowNewLines: false)
                ? (Result<string?>)CrmErrors.PersonEmailInvalid
            : MailAddress.TryCreate(trimmed, out var parsed)
            && string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase)
                ? Result.Success<string?>(trimmed)
            : CrmErrors.PersonEmailInvalid;
    }

    public static bool ContainsInvalidControlChars(string text, bool allowNewLines)
    {
        foreach (var ch in text)
        {
            if (ch == '\0')
            {
                return true;
            }

            if (char.IsControl(ch) && (!allowNewLines || ch is not ('\r' or '\n' or '\t')))
            {
                return true;
            }
        }

        return false;
    }
}
