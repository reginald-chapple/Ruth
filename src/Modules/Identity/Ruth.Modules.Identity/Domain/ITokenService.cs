namespace Ruth.Modules.Identity.Domain;

/// <summary>Issues signed JWT access tokens and cryptographically secure opaque tokens.</summary>
public interface ITokenService
{
    /// <summary>Generates a signed JWT access token and its UTC expiration timestamp.</summary>
    (string AccessToken, DateTimeOffset ExpiresOnUtc) CreateAccessToken(
        UserAccount account,
        IReadOnlyList<SecurityRole> roles,
        DateTimeOffset issuedAtUtc
    );

    /// <summary>Generates a cryptographically random URL-safe opaque token string.</summary>
    string GenerateOpaqueToken(int byteLength = 32);

    /// <summary>Computes a deterministic SHA-256 hex hash of an opaque token or API key.</summary>
    string ComputeSha256Hash(string plaintextToken);

    /// <summary>Computes a PKCE S256 code challenge from a code verifier.</summary>
    string ComputePkceChallenge(string codeVerifier);

    /// <summary>Gets the configured refresh token lifetime.</summary>
    TimeSpan RefreshTokenLifetime { get; }
}
