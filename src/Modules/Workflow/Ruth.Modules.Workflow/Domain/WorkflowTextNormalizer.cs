using System.Text.RegularExpressions;

namespace Ruth.Modules.Workflow.Domain;

internal static partial class WorkflowTextNormalizer
{
    public static bool TryNormalizeSlug(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var candidate = raw.Trim().ToLowerInvariant();
        if (candidate.Length > 96 || !SlugPattern.IsMatch(candidate))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    public static string? TrimOrNull(string? value, int maxLength = 2000)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    [GeneratedRegex(
        "^[a-z0-9]+(?:[-._][a-z0-9]+)*$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex SlugPattern { get; }
}
