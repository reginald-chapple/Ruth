namespace Ruth.Host.Configuration;

/// <summary>Configuration options for JWT token signing.</summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section path for JWT authentication settings.</summary>
    public const string SectionName = "Authentication:Jwt";

    /// <summary>Known local-development placeholder signing key from .env.example.</summary>
    public const string DevelopmentDefaultSigningKey =
        "replace-with-a-unique-random-32-plus-character-secret-in-production";

    /// <summary>Minimum required character length for production signing keys.</summary>
    public const int MinimumSigningKeyLength = 32;

    private static readonly string[] _signingKeyPriorityKeys = ["Authentication:Jwt:SigningKey"];

    /// <summary>Gets or sets the symmetric HMAC signing key.</summary>
    public string? SigningKey { get; set; }

    /// <summary>
    /// Resolves the active signing key using <see cref="ConfigurationValueResolver" />
    /// so explicit null or whitespace overrides shadow lower-priority providers consistently.
    /// </summary>
    public static string? ResolveSigningKey(IConfiguration configuration)
    {
        return ConfigurationValueResolver.ResolveFirstNonEmpty(
            configuration,
            _signingKeyPriorityKeys
        );
    }
}
