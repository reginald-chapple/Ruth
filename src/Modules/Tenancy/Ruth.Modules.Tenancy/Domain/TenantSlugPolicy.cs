using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Tenancy.Domain;

internal static class TenantSlugPolicy
{
    private static readonly HashSet<string> _reservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "api",
        "app",
        "assets",
        "auth",
        "billing",
        "cdn",
        "console",
        "dashboard",
        "docs",
        "health",
        "help",
        "localhost",
        "login",
        "mail",
        "metrics",
        "onboarding",
        "ops",
        "platform",
        "portal",
        "root",
        "ruth",
        "sandbox",
        "security",
        "signup",
        "smtp",
        "static",
        "status",
        "stripe",
        "support",
        "system",
        "webhooks",
        "www",
    };

    private static readonly HashSet<string> _eligibleSubsections = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "501(c)(3)",
    };

    public static Result<string> NormalizeAndValidateSlug(string? rawSlug)
    {
        if (string.IsNullOrWhiteSpace(rawSlug))
        {
            return TenancyErrors.InvalidSlug;
        }

        var normalized = rawSlug.Trim().ToLowerInvariant();
        if (
            normalized.Length is < 3 or > 50
            || normalized[0] == '-'
            || normalized[^1] == '-'
            || normalized.Contains("--", StringComparison.Ordinal)
            || _reservedSlugs.Contains(normalized)
        )
        {
            return TenancyErrors.InvalidSlug;
        }

        foreach (var ch in normalized)
        {
            if (!char.IsAsciiLetterLower(ch) && !char.IsAsciiDigit(ch) && ch != '-')
            {
                return TenancyErrors.InvalidSlug;
            }
        }

        return normalized;
    }

    public static Result<string> NormalizeAndValidateSubsection(string? subsectionCode)
    {
        if (string.IsNullOrWhiteSpace(subsectionCode))
        {
            return TenancyErrors.IneligibleTaxExemptSubsection;
        }

        var normalized = subsectionCode.Trim();
        return _eligibleSubsections.Contains(normalized)
            ? "501(c)(3)"
            : TenancyErrors.IneligibleTaxExemptSubsection;
    }

    public static Result<string> NormalizeAndValidateHostname(
        string? rawHostname,
        string reservedBaseDomain
    )
    {
        if (string.IsNullOrWhiteSpace(rawHostname))
        {
            return TenancyErrors.InvalidHostname;
        }

        var normalized = StripPort(rawHostname.Trim().ToLowerInvariant());
        var isReservedOrInvalid =
            normalized.Length is < 4 or > 253
            || !normalized.Contains('.', StringComparison.Ordinal)
            || normalized.StartsWith('.')
            || normalized.EndsWith('.')
            || string.Equals(normalized, "localhost", StringComparison.Ordinal)
            || normalized.EndsWith($".{reservedBaseDomain}", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, reservedBaseDomain, StringComparison.OrdinalIgnoreCase);

        return !isReservedOrInvalid && Uri.CheckHostName(normalized) == UriHostNameType.Dns
            ? normalized
            : TenancyErrors.InvalidHostname;
    }

    public static string StripPort(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        var trimmed = host.Trim().ToLowerInvariant();
        var colonIndex = trimmed.IndexOf(':', StringComparison.Ordinal);
        return colonIndex >= 0 ? trimmed[..colonIndex] : trimmed;
    }
}
