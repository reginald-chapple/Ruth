using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Security;

internal sealed class JwtTokenService(IOptions<IdentityJwtOptions> options) : ITokenService
{
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

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
            claims.Add(new Claim(RuthClaimTypes.RoleId, role.Id.Value.ToString("D")));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issued.UtcDateTime,
            NotBefore = issued.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = credentials,
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

    public string ComputePkceChallenge(string codeVerifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
