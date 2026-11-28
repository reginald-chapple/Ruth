using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Identity.Infrastructure.Security;

internal sealed class JwtTokenService(IOptions<IdentityJwtOptions> options) : ITokenService
{
    private const string SaltedApiKeyPrefix = "$pbkdf2-s256$";
    private const int ApiKeySaltBytes = 16;
    private const int ApiKeyDerivedBytes = 32;
    private const int ApiKeyPbkdf2Iterations = 100_000;

    private readonly IdentityJwtOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));

    private readonly JsonWebTokenHandler _tokenHandler = new();

    public TimeSpan RefreshTokenLifetime =>
        TimeSpan.FromDays(Math.Max(1, _options.RefreshTokenLifetimeDays));

    public (string AccessToken, DateTimeOffset ExpiresOnUtc) CreateAccessToken(
        UserAccount account,
        IReadOnlyList<SecurityRole> roles,
        DateTimeOffset issuedAtUtc
    )
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(roles);

        var issued = issuedAtUtc.ToUniversalTime();
        var expires = issued.AddMinutes(Math.Max(1, _options.AccessTokenLifetimeMinutes));
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.ResolveEffectiveSigningKey())
        );
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = BuildAccessClaims(account, roles, issued);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issued.UtcDateTime,
            NotBefore = issued.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = credentials,
            TokenType = "JWT",
        };

        var token = _tokenHandler.CreateToken(descriptor);
        return (token, expires);
    }

    public string GenerateOpaqueToken(int byteLength = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(Math.Max(16, byteLength));
        return Base64UrlEncode(bytes);
    }

    public string ComputeSha256Hash(string plaintextToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextToken);
        var bytes = Encoding.UTF8.GetBytes(plaintextToken.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    public string HashApiKeyWithSalt(string plaintextApiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextApiKey);
        var salt = RandomNumberGenerator.GetBytes(ApiKeySaltBytes);
        var derived = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plaintextApiKey.Trim()),
            salt,
            ApiKeyPbkdf2Iterations,
            HashAlgorithmName.SHA256,
            ApiKeyDerivedBytes
        );
        var iterText = ApiKeyPbkdf2Iterations.ToString(CultureInfo.InvariantCulture);
        return $"{SaltedApiKeyPrefix}{iterText}${Convert.ToHexStringLower(salt)}${Convert.ToHexStringLower(derived)}";
    }

    public bool VerifyApiKeyHash(string plaintextApiKey, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(plaintextApiKey) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var trimmedKey = plaintextApiKey.Trim();
        var trimmedStored = storedHash.Trim();

        return trimmedStored.StartsWith(SaltedApiKeyPrefix, StringComparison.Ordinal)
            ? VerifySaltedPbkdf2Hash(trimmedKey, trimmedStored)
            : trimmedStored.Length == 64
                && TryDecodeHex(trimmedStored, out var expectedRaw)
                && CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(Encoding.UTF8.GetBytes(trimmedKey)),
                    expectedRaw
                );
    }

    public string ComputePkceChallenge(string codeVerifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static List<Claim> BuildAccessClaims(
        UserAccount account,
        IReadOnlyList<SecurityRole> roles,
        DateTimeOffset issued
    )
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.Value.ToString("D")),
            new(ClaimTypes.NameIdentifier, account.Id.Value.ToString("D")),
            new(JwtRegisteredClaimNames.UniqueName, account.UserName),
            new(ClaimTypes.Name, account.UserName),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(ClaimTypes.Email, account.Email),
            new(RuthClaimTypes.DisplayName, account.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString("D")),
            new(
                JwtRegisteredClaimNames.Iat,
                issued.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64
            ),
        };

        var effectiveTenantId = !account.TenantId.IsEmpty
            ? account.TenantId
            : AmbientTenantContext.CurrentTenantId;
        if (!effectiveTenantId.IsEmpty)
        {
            claims.Add(new Claim(RuthClaimTypes.TenantId, effectiveTenantId.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
            claims.Add(new Claim(RuthClaimTypes.RoleId, role.Id.Value.ToString("D")));
        }

        return claims;
    }

    private static bool VerifySaltedPbkdf2Hash(string trimmedKey, string trimmedStored)
    {
        var parts = trimmedStored[SaltedApiKeyPrefix.Length..].Split('$');
        if (
            parts.Length != 3
            || !int.TryParse(
                parts[0],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var iterations
            )
            || iterations is < 10_000 or > 1_000_000
            || !TryDecodeHex(parts[1], out var salt)
            || salt.Length < ApiKeySaltBytes
            || !TryDecodeHex(parts[2], out var expectedHash)
            || expectedHash.Length != ApiKeyDerivedBytes
        )
        {
            return false;
        }

        var computed = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(trimmedKey),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length
        );
        return CryptographicOperations.FixedTimeEquals(computed, expectedHash);
    }

    private static bool TryDecodeHex(string hex, out byte[] bytes)
    {
        bytes = [];
        if (hex.Length == 0 || (hex.Length & 1) != 0)
        {
            return false;
        }

        try
        {
            bytes = Convert.FromHexString(hex);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
