using System.Collections.Frozen;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// Classifies and redacts sensitive credential keys inside provider configuration dictionaries
/// so secrets are encrypted at rest and masked in API/UI read responses.
/// </summary>
public static class SensitiveSettingRedactor
{
    /// <summary>Canonical mask returned in API/UI responses in place of stored credential values.</summary>
    public const string RedactedMask = "********";

    private static readonly FrozenSet<string> _nonSensitiveKeyAllowlist = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "publickey",
        "public_key",
        "publictokenizerkey",
        "public_tokenizer_key",
        "publishablekey",
        "publishable_key",
        "providerkey",
        "provider_key",
        "topickey",
        "topic_key",
        "layoutkey",
        "layout_key",
        "idempotencykey",
        "idempotency_key",
        "statement_descriptor",
        "statementdescriptor",
        "region",
        "domain",
        "host",
        "port",
        "endpoint",
        "baseurl",
        "base_url"
    );

    private static readonly string[] _sensitiveSubstrings =
    [
        "secret",
        "password",
        "passwd",
        "apikey",
        "api_key",
        "api-key",
        "securitykey",
        "security_key",
        "security-key",
        "privatekey",
        "private_key",
        "private-key",
        "authtoken",
        "auth_token",
        "auth-token",
        "accesstoken",
        "access_token",
        "access-token",
        "refreshtoken",
        "refresh_token",
        "signingkey",
        "signing_key",
        "webhookkey",
        "webhook_key",
        "passphrase",
        "credential",
        "connectionstring",
        "bearer",
        "accesskey",
        "access_key",
    ];

    /// <summary>Determines whether <paramref name="settingKey" /> names a secret credential requiring encryption and masking.</summary>
    public static bool IsSensitiveKey(string? settingKey)
    {
        if (string.IsNullOrWhiteSpace(settingKey))
        {
            return false;
        }

        var trimmed = settingKey.Trim();
        if (_nonSensitiveKeyAllowlist.Contains(trimmed))
        {
            return false;
        }

        foreach (var marker in _sensitiveSubstrings)
        {
            if (trimmed.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return string.Equals(trimmed, "token", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "key", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "pwd", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("_token", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith("-token", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns a copy of <paramref name="settings" /> with sensitive values masked as <see cref="RedactedMask" />.</summary>
    public static IReadOnlyDictionary<string, string> RedactDictionary(
        IReadOnlyDictionary<string, string>? settings,
        bool redactAllValues = false
    )
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (settings is null)
        {
            return result;
        }

        foreach (var pair in settings)
        {
            var shouldMask =
                !string.IsNullOrEmpty(pair.Value) && (redactAllValues || IsSensitiveKey(pair.Key));
            result[pair.Key] = shouldMask ? RedactedMask : pair.Value;
        }

        return result;
    }

    /// <summary>
    /// Merges incoming <paramref name="incoming" /> settings with <paramref name="existing" />,
    /// preserving existing unmasked secrets when the caller submits <see cref="RedactedMask" />.
    /// </summary>
    public static IReadOnlyDictionary<string, string> MergePreservingRedactedSecrets(
        IReadOnlyDictionary<string, string>? incoming,
        IReadOnlyDictionary<string, string>? existing
    )
    {
        if (incoming is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in incoming)
        {
            merged[pair.Key] =
                string.Equals(pair.Value, RedactedMask, StringComparison.Ordinal)
                && existing is not null
                && existing.TryGetValue(pair.Key, out var priorValue)
                    ? priorValue
                    : pair.Value;
        }

        return merged;
    }
}
