namespace Ruth.Modules.Identity.Infrastructure.Security;

/// <summary>JWT signing and token lifetime configuration for the Identity module.</summary>
public sealed class IdentityJwtOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Authentication:Jwt";

    /// <summary>Default local development fallback signing key matching .env.example.</summary>
    public const string DevelopmentFallbackSigningKey =
        "replace-with-a-unique-random-32-plus-character-secret-in-production";

    /// <summary>Gets or sets the symmetric HMAC-SHA256 signing key.</summary>
    public string? SigningKey { get; set; }

    /// <summary>Gets or sets the JWT issuer (iss).</summary>
    public string Issuer { get; set; } = "ruth.identity";

    /// <summary>Gets or sets the JWT audience (aud).</summary>
    public string Audience { get; set; } = "ruth.api";

    /// <summary>Gets or sets the access token lifetime in minutes.</summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 30;

    /// <summary>Gets or sets the refresh token lifetime in days.</summary>
    public int RefreshTokenLifetimeDays { get; set; } = 14;

    /// <summary>Resolves the effective signing key, falling back only when unset in non-production.</summary>
    public string ResolveEffectiveSigningKey()
    {
        return string.IsNullOrWhiteSpace(SigningKey)
            ? DevelopmentFallbackSigningKey
            : SigningKey.Trim();
    }
}
