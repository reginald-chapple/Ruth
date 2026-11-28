using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// Constant-time HMAC-SHA256 signature and bounded replay-window verifier for inbound provider webhooks.
/// </summary>
public static class WebhookHmacReplayVerifier
{
    /// <summary>Default maximum allowed clock drift / replay age (5 minutes) for timestamped webhooks.</summary>
    public static readonly TimeSpan DefaultReplayTolerance = TimeSpan.FromMinutes(5);

    /// <summary>Formats a standard <c lang="text">t=&lt;unix&gt;,v1=&lt;hex&gt;</c> signed webhook header.</summary>
    public static string CreateTimestampedSignatureHeader(
        string rawPayload,
        string secret,
        DateTimeOffset timestampUtc,
        string signatureKey = "v1"
    )
    {
        ArgumentNullException.ThrowIfNull(rawPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(signatureKey);

        var unixSeconds = timestampUtc.ToUnixTimeSeconds();
        var signedPayload = string.Create(
            CultureInfo.InvariantCulture,
            $"{unixSeconds}.{rawPayload}"
        );
        var hex = Convert.ToHexStringLower(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes(signedPayload)
            )
        );
        return string.Create(CultureInfo.InvariantCulture, $"t={unixSeconds},{signatureKey}={hex}");
    }

    /// <summary>
    /// Verifies a <c lang="text">t=&lt;unix&gt;,&lt;sigKey&gt;=&lt;hex&gt;</c> header in constant time and enforces <paramref name="tolerance" /> around <paramref name="utcNow" />.
    /// </summary>
    public static bool VerifyTimestampedHeader(
        string? rawPayload,
        string? signatureHeader,
        string? secret,
        DateTimeOffset utcNow,
        string signatureKey = "v1",
        TimeSpan? tolerance = null
    )
    {
        if (
            string.IsNullOrWhiteSpace(rawPayload)
            || string.IsNullOrWhiteSpace(signatureHeader)
            || string.IsNullOrWhiteSpace(secret)
        )
        {
            return false;
        }

        if (
            !TryExtractTimestampAndSignature(
                signatureHeader,
                signatureKey,
                out var unixTs,
                out var sigHex
            )
        )
        {
            return false;
        }

        if (!IsWithinReplayWindow(unixTs, utcNow, tolerance ?? DefaultReplayTolerance))
        {
            return false;
        }

        var signedPayload = string.Create(CultureInfo.InvariantCulture, $"{unixTs}.{rawPayload}");
        return VerifyRawHmacHexConstantTime(signedPayload, sigHex, secret);
    }

    /// <summary>Checks whether <paramref name="unixTimestampSeconds" /> falls within <paramref name="tolerance" /> of <paramref name="utcNow" />.</summary>
    public static bool IsWithinReplayWindow(
        long unixTimestampSeconds,
        DateTimeOffset utcNow,
        TimeSpan tolerance
    )
    {
        if (unixTimestampSeconds <= 0 || tolerance <= TimeSpan.Zero)
        {
            return false;
        }

        DateTimeOffset issuedAt;
        try
        {
            issuedAt = DateTimeOffset.FromUnixTimeSeconds(unixTimestampSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        var delta = (utcNow.ToUniversalTime() - issuedAt).Duration();
        return delta <= tolerance;
    }

    /// <summary>Verifies an unprefixed hex-encoded HMAC-SHA256 digest in constant time on raw 32-byte digests.</summary>
    public static bool VerifyRawHmacHexConstantTime(
        string signedContent,
        ReadOnlySpan<char> providedHexSignature,
        string secret
    )
    {
        if (string.IsNullOrEmpty(signedContent) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var trimmed = providedHexSignature.Trim();
        if (trimmed.Length != 64)
        {
            return false;
        }

        Span<byte> providedBytes = stackalloc byte[32];
        if (
            Convert.FromHexString(trimmed, providedBytes, out _, out _)
            != System.Buffers.OperationStatus.Done
        )
        {
            return false;
        }

        Span<byte> expectedBytes = stackalloc byte[32];
        _ = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(signedContent),
            expectedBytes
        );

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private static bool TryExtractTimestampAndSignature(
        string header,
        string signatureKey,
        out long unixTimestampSeconds,
        out string signatureHex
    )
    {
        unixTimestampSeconds = 0;
        signatureHex = string.Empty;
        var keyPrefix = $"{signatureKey}=";

        foreach (var part in header.Split(',', StringSplitOptions.TrimEntries))
        {
            if (
                part.StartsWith("t=", StringComparison.Ordinal)
                && long.TryParse(
                    part.AsSpan(2),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var parsedTs
                )
            )
            {
                unixTimestampSeconds = parsedTs;
            }
            else if (
                signatureHex.Length == 0
                && part.StartsWith(keyPrefix, StringComparison.Ordinal)
            )
            {
                signatureHex = part[keyPrefix.Length..];
            }
        }

        return unixTimestampSeconds > 0 && signatureHex.Length > 0;
    }
}
