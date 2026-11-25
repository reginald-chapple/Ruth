using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain;

internal static class CheckInTextPolicy
{
    public const int NameMaxLength = 120;
    public const int DescriptionMaxLength = 2000;
    public const int TokenMaxLength = 80;

    public static Result<string> NormalizeRequired(
        string? value,
        int maxLength,
        DomainError requiredError,
        DomainError tooLongError
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<string>(requiredError);
        }

        if (ContainsInvalidControlChars(value, allowNewLines: false))
        {
            return Result.Failure<string>(CheckInErrors.InvalidControlCharacters);
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength
            ? Result.Failure<string>(tooLongError)
            : Result.Success(trimmed);
    }

    public static Result<string?> NormalizeOptional(
        string? value,
        int maxLength,
        bool allowNewLines = true
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<string?>(value: null);
        }

        if (ContainsInvalidControlChars(value, allowNewLines))
        {
            return Result.Failure<string?>(CheckInErrors.InvalidControlCharacters);
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength
            ? Result.Failure<string?>(CheckInErrors.DescriptionTooLong)
            : Result.Success<string?>(trimmed);
    }

    public static Result<IReadOnlyList<string>> NormalizeTokens(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Result.Success<IReadOnlyList<string>>([]);
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<string>(values.Count);
        foreach (var raw in values)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (!IsValidSingleToken(raw, out var trimmed))
            {
                return Result.Failure<IReadOnlyList<string>>(CheckInErrors.TierOrCodeInvalid);
            }

            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return Result.Success<IReadOnlyList<string>>(normalized);
    }

    public static bool ContainsInvalidControlChars(string value, bool allowNewLines)
    {
        foreach (var ch in value)
        {
            if (!IsPermittedChar(ch, allowNewLines))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidSingleToken(string raw, out string trimmed)
    {
        trimmed = raw.Trim();
        return !ContainsInvalidControlChars(raw, allowNewLines: false)
            && trimmed.Length <= TokenMaxLength;
    }

    private static bool IsPermittedChar(char ch, bool allowNewLines)
    {
        return !char.IsControl(ch) || (allowNewLines && ch is '\r' or '\n' or '\t');
    }
}
