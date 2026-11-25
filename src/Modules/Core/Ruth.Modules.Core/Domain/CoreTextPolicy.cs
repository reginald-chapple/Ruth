using System.Globalization;
using System.Text.RegularExpressions;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain;

internal static partial class CoreTextPolicy
{
    public const int NameMaxLength = 100;
    public const int DefinedValueMaxLength = 250;
    public const int DescriptionMaxLength = 2000;
    public const int AttributeKeyMaxLength = 100;

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
            : ContainsDisallowedControlChars(trimmed, allowNewLines: false)
                ? CoreErrors.InvalidControlCharacters
            : trimmed;
    }

    public static Result<string?> ValidateOptionalText(
        string? value,
        int maxLength,
        bool allowNewLines = true
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<string?>(value: null);
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength
                ? DomainError.Validation(
                    "Core.TextTooLong",
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Text cannot exceed {maxLength} characters."
                    )
                )
            : ContainsDisallowedControlChars(trimmed, allowNewLines)
                ? CoreErrors.InvalidControlCharacters
            : Result.Success<string?>(trimmed);
    }

    public static Result<string> ValidateAttributeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return CoreErrors.AttributeKeyInvalid;
        }

        var trimmed = key.Trim();
        return trimmed.Length <= AttributeKeyMaxLength && AttributeKeyRegex.IsMatch(trimmed)
            ? trimmed
            : CoreErrors.AttributeKeyInvalid;
    }

    private static bool ContainsDisallowedControlChars(string text, bool allowNewLines)
    {
        foreach (var ch in text)
        {
            if (char.IsControl(ch))
            {
                if (allowNewLines && ch is '\r' or '\n' or '\t')
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(
        "^[A-Za-z][A-Za-z0-9_.-]*$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex AttributeKeyRegex { get; }
}
