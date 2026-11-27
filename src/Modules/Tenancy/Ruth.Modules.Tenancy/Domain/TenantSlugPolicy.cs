using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Tenancy.Domain;

internal static class TenantSlugPolicy
{
    public static Result<string> NormalizeAndValidateSlug(string? rawSlug)
    {
        if (string.IsNullOrWhiteSpace(rawSlug))
        {
            return TenancyErrors.InvalidSlug;
        }

        var normalized = rawSlug.Trim().ToLowerInvariant();
        return IsSlugStructureValid(normalized) && ContainsOnlyValidSlugChars(normalized)
            ? normalized
            : TenancyErrors.InvalidSlug;
    }

    public static Result<string> NormalizeAndValidateSubsection(string? subsectionCode)
    {
        return
            !string.IsNullOrWhiteSpace(subsectionCode)
            && TenantPolicyLookupData.EligibleSubsections.TryGetValue(
                subsectionCode.Trim(),
                out var canonical
            )
            ? canonical
            : TenancyErrors.IneligibleTaxExemptSubsection;
    }

    public static Result<string> NormalizeAndValidateStateCode(string? rawStateCode)
    {
        if (string.IsNullOrWhiteSpace(rawStateCode))
        {
            return TenancyErrors.InvalidStateCode;
        }

        var trimmed = rawStateCode.Trim().ToUpperInvariant();
        return TenantPolicyLookupData.ValidUsJurisdictionCodes.Contains(trimmed)
            ? trimmed
            : TenancyErrors.InvalidStateCode;
    }

    public static Result<string> NormalizeAndValidateTimeZoneId(string? rawTimeZoneId)
    {
        if (string.IsNullOrWhiteSpace(rawTimeZoneId))
        {
            return "UTC";
        }

        var trimmed = rawTimeZoneId.Trim();
        return trimmed.Length <= 100 && TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out _)
            ? trimmed
            : TenancyErrors.InvalidTimeZone;
    }

    public static Result<string> NormalizeAndValidateOnboardingUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return TenancyErrors.InvalidOnboardingUrls;
        }

        var trimmed = rawUrl.Trim();
        return IsSafeRelativePath(trimmed) || IsHttpOrHttpsAbsoluteUrl(trimmed)
            ? trimmed
            : TenancyErrors.InvalidOnboardingUrls;
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
        return
            HasValidHostnameShape(normalized)
            && !IsReservedDomain(normalized, reservedBaseDomain)
            && Uri.CheckHostName(normalized) == UriHostNameType.Dns
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

    private static bool IsSlugStructureValid(string normalized)
    {
        return normalized.Length is >= 3 and <= 50
            && normalized[0] != '-'
            && normalized[^1] != '-'
            && !normalized.Contains("--", StringComparison.Ordinal)
            && !TenantPolicyLookupData.ReservedSlugs.Contains(normalized);
    }

    private static bool ContainsOnlyValidSlugChars(string normalized)
    {
        foreach (var ch in normalized)
        {
            if (!char.IsAsciiLetterLower(ch) && !char.IsAsciiDigit(ch) && ch != '-')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSafeRelativePath(string url)
    {
        return url.StartsWith('/')
            && !url.StartsWith("//", StringComparison.Ordinal)
            && !url.StartsWith("/\\", StringComparison.Ordinal);
    }

    private static bool IsHttpOrHttpsAbsoluteUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var absolute)
            && (
                string.Equals(
                    absolute.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase
                )
                || string.Equals(
                    absolute.Scheme,
                    Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase
                )
            );
    }

    private static bool HasValidHostnameShape(string normalized)
    {
        return normalized.Length is >= 4 and <= 253
            && normalized.Contains('.', StringComparison.Ordinal)
            && !normalized.StartsWith('.')
            && !normalized.EndsWith('.');
    }

    private static bool IsReservedDomain(string normalized, string reservedBaseDomain)
    {
        return string.Equals(normalized, "localhost", StringComparison.Ordinal)
            || normalized.EndsWith($".{reservedBaseDomain}", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, reservedBaseDomain, StringComparison.OrdinalIgnoreCase);
    }
}
