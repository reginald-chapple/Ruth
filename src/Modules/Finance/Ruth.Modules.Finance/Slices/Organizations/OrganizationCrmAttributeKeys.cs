namespace Ruth.Modules.Finance.Slices.Organizations;

internal static class OrganizationCrmAttributeKeys
{
    public const string TaxIdentifier = "finance.org.taxId";
    public const string SponsorTier = "finance.org.sponsorTier";
    public const string WebsiteUrl = "finance.org.websiteUrl";
    public const string BillingNotes = "finance.org.billingNotes";

    public static Dictionary<string, string?> BuildAttributeDictionary(
        string? taxIdentifier,
        string? sponsorTier,
        string? websiteUrl,
        string? billingNotes
    )
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        SetIfPresent(dict, TaxIdentifier, taxIdentifier);
        SetIfPresent(dict, SponsorTier, sponsorTier);
        SetIfPresent(dict, WebsiteUrl, websiteUrl);
        SetIfPresent(dict, BillingNotes, billingNotes);
        return dict;
    }

    public static string? GetValue(IReadOnlyDictionary<string, string?> attributes, string key)
    {
        return attributes.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val)
            ? val
            : null;
    }

    private static void SetIfPresent(
        Dictionary<string, string?> target,
        string key,
        string? rawValue
    )
    {
        target[key] = string.IsNullOrWhiteSpace(rawValue) ? null : rawValue.Trim();
    }
}
