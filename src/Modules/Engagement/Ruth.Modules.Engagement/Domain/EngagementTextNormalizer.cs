namespace Ruth.Modules.Engagement.Domain;

internal static class EngagementTextNormalizer
{
    public static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }

    public static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    public static void ApplyAttributes(
        Dictionary<string, string?> target,
        IReadOnlyDictionary<string, string?>? source
    )
    {
        if (source is null)
        {
            return;
        }

        target.Clear();
        foreach (var (key, value) in source)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                target[key.Trim()] = value;
            }
        }
    }
}
