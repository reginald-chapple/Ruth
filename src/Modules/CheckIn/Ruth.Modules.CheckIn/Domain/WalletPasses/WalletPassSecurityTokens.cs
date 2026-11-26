using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Ruth.Modules.CheckIn.Domain.WalletPasses;

/// <summary>Cryptographic token generation, deterministic PassKit KDF, and constant-time hash verification helpers.</summary>
public static class WalletPassSecurityTokens
{
    private const string ApplePassSchemePrefix = "ApplePass ";

    /// <summary>Generates a collision-free uppercase pass serial number.</summary>
    public static string GenerateSerialNumber()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return $"RWP-{Convert.ToHexString(bytes)}";
    }

    /// <summary>Generates a collision-free Kiosk barcode scan token prefixed with WLT-.</summary>
    public static string GenerateScanToken()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return $"WLT-{Convert.ToHexString(bytes)}";
    }

    /// <summary>Generates a 16-byte lowercase hex salt for PassKit auth token derivation.</summary>
    public static string GenerateSaltHex()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexStringLower(bytes);
    }

    /// <summary>Generates a URL-safe 24-byte random claim token.</summary>
    public static string GenerateClaimToken()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Base64Url.EncodeToString(bytes);
    }

    /// <summary>Derives the 64-character PassKit authenticationToken using HMAC-SHA256 without storing plaintext in PostgreSQL.</summary>
    public static string DeriveAppleAuthToken(
        string serverSecret,
        Guid issuedPassId,
        string authSaltHex
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(authSaltHex);

        var message = $"ruth-passkit:{issuedPassId:N}:{authSaltHex}";
        var mac = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(serverSecret),
            Encoding.UTF8.GetBytes(message)
        );
        return Convert.ToHexStringLower(mac);
    }

    /// <summary>Computes the lowercase SHA-256 hex digest of a token.</summary>
    public static string ComputeSha256Hex(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim()));
        return Convert.ToHexStringLower(digest);
    }

    /// <summary>Performs constant-time SHA-256 verification of a candidate token against an expected 64-char hex digest.</summary>
    public static bool ConstantTimeMatchesHash(string? candidateRawToken, string expectedSha256Hex)
    {
        if (string.IsNullOrWhiteSpace(candidateRawToken) || expectedSha256Hex.Length != 64)
        {
            return false;
        }

        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedSha256Hex);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualBytes = SHA256.HashData(Encoding.UTF8.GetBytes(candidateRawToken.Trim()));
        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    /// <summary>Extracts the raw token from an HTTP Authorization: ApplePass header value.</summary>
    public static string? ExtractApplePassHeaderToken(string? authorizationHeader)
    {
        if (
            string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith(
                ApplePassSchemePrefix,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return null;
        }

        var token = authorizationHeader[ApplePassSchemePrefix.Length..].Trim();
        return token.Length >= 16 ? token : null;
    }
}
