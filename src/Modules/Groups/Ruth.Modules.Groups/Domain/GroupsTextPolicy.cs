using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain;

internal static class GroupsTextPolicy
{
    public const int NameMaxLength = 100;
    public const int ShortLabelMaxLength = 150;
    public const int DescriptionMaxLength = 2000;
    public const int NoteMaxLength = 2000;

    public static Result<string> NormalizeRequired(string? value, int maxLength = NameMaxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GroupsErrors.NameRequired;
        }

        var trimmed = value.Trim();
        return
            trimmed.Length > maxLength
            || ContainsDisallowedControlChars(trimmed, allowNewLines: false)
            ? (Result<string>)GroupsErrors.InvalidText
            : Result.Success(trimmed);
    }

    public static Result<string?> NormalizeOptional(
        string? value,
        int maxLength = DescriptionMaxLength,
        bool allowNewLines = true
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<string?>(value: null);
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength || ContainsDisallowedControlChars(trimmed, allowNewLines)
            ? (Result<string?>)GroupsErrors.InvalidText
            : Result.Success<string?>(trimmed);
    }

    private static bool ContainsDisallowedControlChars(string value, bool allowNewLines)
    {
        foreach (var ch in value)
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
}
