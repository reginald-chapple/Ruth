using System.Buffers.Text;
using System.Security.Cryptography;

namespace Ruth.Modules.VirtualRooms.Contracts.Providers;

internal static class LiveKitJwtEncodingHelper
{
    public static bool MatchesBodySha256(string? claimedSha, ReadOnlySpan<byte> rawBodyUtf8)
    {
        if (string.IsNullOrWhiteSpace(claimedSha))
        {
            return false;
        }

        Span<byte> actualDigest = stackalloc byte[32];
        _ = SHA256.HashData(rawBodyUtf8, actualDigest);

        return TryDecodeDigest(claimedSha.Trim(), out var claimedBytes)
            && CryptographicOperations.FixedTimeEquals(claimedBytes, actualDigest);
    }

    public static string? ExtractBearerOrRawToken(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        var trimmed = header.Trim();
        return trimmed.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? trimmed[7..].Trim()
            : trimmed;
    }

    public static bool TrySplitJwt(
        string token,
        out string header,
        out string payload,
        out string signature
    )
    {
        header = string.Empty;
        payload = string.Empty;
        signature = string.Empty;
        var firstDot = token.IndexOf('.', StringComparison.Ordinal);
        var segments = token.Split('.');
        if (
            firstDot <= 0
            || segments.Length != 3
            || segments[1].Length == 0
            || segments[2].Length == 0
        )
        {
            return false;
        }

        header = segments[0];
        payload = segments[1];
        signature = segments[2];
        return true;
    }

    private static bool TryDecodeDigest(string raw, out byte[] bytes)
    {
        if (raw.Length == 64)
        {
            try
            {
                bytes = Convert.FromHexString(raw);
                return true;
            }
            catch (FormatException)
            {
                bytes = [];
                return false;
            }
        }

        try
        {
            bytes = Convert.FromBase64String(raw);
            return true;
        }
        catch (FormatException)
        {
            try
            {
                bytes = Base64Url.DecodeFromChars(raw);
                return true;
            }
            catch (FormatException)
            {
                bytes = [];
                return false;
            }
        }
    }
}
